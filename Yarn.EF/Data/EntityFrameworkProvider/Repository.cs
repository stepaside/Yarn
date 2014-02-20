using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Extensions;
using Yarn.Reflection;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILazyLoader
    {
        protected IDataContext<DbContext> _context;

        protected readonly string _prefix;
        protected readonly bool _lazyLoadingEnabled;
        protected readonly bool _proxyCreationEnabled;
        protected readonly bool _autoDetectChangesEnabled;
        protected readonly bool _validateOnSaveEnabled;
        protected readonly bool _migrationEnabled;

        private ConcurrentDictionary<Type, DbSet> _dbSets;

        public Repository() : this(prefix: null) { }

        public Repository(string prefix = null, 
                            bool lazyLoadingEnabled = true,
                            bool proxyCreationEnabled = false,
                            bool autoDetectChangesEnabled = false,
                            bool validateOnSaveEnabled = true,
                            bool migrationEnabled = false) 
        {
            _prefix = prefix;
            _lazyLoadingEnabled = lazyLoadingEnabled;
            _proxyCreationEnabled = proxyCreationEnabled;
            _autoDetectChangesEnabled = autoDetectChangesEnabled;
            _validateOnSaveEnabled = validateOnSaveEnabled;
            _migrationEnabled = migrationEnabled;
            _dbSets = new ConcurrentDictionary<Type, DbSet>();
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return this.Table<T>().Find(id);
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
            var idSelector = Expression.Lambda<Func<T, ID>>(body, parameter);

            var predicate = idSelector.BuildOrExpression<T, ID>(ids);

            return this.Table<T>().Where(predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.Table<T>().FirstOrDefault(criteria);
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }
        
        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = this.Table<T>().Where(criteria);
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = criteria.Apply(Table<T>());
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return PrepareSqlQuery<T>(command, parameters).ToArray();
        }

        protected DbRawSqlQuery<T> PrepareSqlQuery<T>(string command, ParamList parameters) where T : class
        {
            var connection = this.DbContext.Database.Connection;
            var query = parameters != null
                ? this.DbContext.Database.SqlQuery<T>(command, parameters.Select(p => DbFactory.CreateParameter(connection, p.Key, p.Value)).ToArray())
                : this.DbContext.Database.SqlQuery<T>(command);
            return query;
        }

        public T Add<T>(T entity) where T : class
        {
            return this.Table<T>().Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return this.Table<T>().Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var result = this.GetById<T, ID>(id);
            if (result != null)
            {
                Remove<T>(result);
            }
            return result;
        }

        public T Update<T>(T entity) where T : class
        {
            this.Table<T>().AddOrUpdate<T>(entity);
            return entity;
        }
        
        public void Attach<T>(T entity) where T : class
        {
            this.Table<T>().Attach(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            ((IObjectContextAdapter)this.DbContext).ObjectContext.Detach(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return this.Table<T>();
        }

        public long Count<T>() where T : class
        {
            return Table<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public IQueryable<TRoot> Include<TRoot, TRelated>(params Expression<Func<TRoot, TRelated>>[] selectors)
            where TRoot : class
            where TRelated : class
        {
            var query = this.All<TRoot>();
            foreach (var selector in selectors)
            {
                query = query.Include<TRoot, TRelated>(selector);
            }
            return query;
        }

        public DbSet<T> Table<T>() where T : class
        {
            var dbSet = _dbSets.GetOrAdd(typeof(T), t => this.DbContext.Set<T>());
            return dbSet.Cast<T>();
        }

        protected DbContext DbContext
        {
            get
            {
                return ((IDataContext<DbContext>)this.DataContext).Session;
            }
        }

        public virtual IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new DataContext(_prefix, _lazyLoadingEnabled, _proxyCreationEnabled, _autoDetectChangesEnabled, _validateOnSaveEnabled, _migrationEnabled);
                }
                return _context;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Session.Dispose();
                    _context = null;
                }
            }
        }

        #region IMetaDataProvider Members

        IEnumerable<string> IMetaDataProvider.GetPrimaryKey<T>()
        {
            return ((IObjectContextAdapter)this.DbContext)
                    .ObjectContext.CreateObjectSet<T>()
                    .EntitySet.ElementType.KeyMembers.Select(k => k.Name);
        
        }

        IDictionary<string, object> IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var values = new Dictionary<string, object>();
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>();
            foreach (var key in primaryKey)
            {
                values[key] = PropertyAccessor.Get(entity, key);
            }
            return values;
        }

        #endregion
    }
}
