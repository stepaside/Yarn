using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider
    {
        protected IDataContext<DbContext> _context;

        protected readonly string _prefix;
        protected readonly bool _lazyLoadingEnabled;
        protected readonly bool _proxyCreationEnabled;
        protected readonly bool _autoDetectChangesEnabled;
        protected readonly bool _validateOnSaveEnabled;
        protected readonly bool _migrationEnabled;
        protected readonly string _nameOrConnectionString;
        protected readonly string _assemblyNameOrLocation;
        protected readonly Assembly _configurationAssembly;
        protected readonly Type _dbContextType;
        protected readonly bool _mergeOnUpdate;

        public Repository() : this(prefix: null) { }

        public Repository(string prefix = null, 
                            bool lazyLoadingEnabled = true,
                            bool proxyCreationEnabled = true,
                            bool autoDetectChangesEnabled = false,
                            bool validateOnSaveEnabled = true,
                            bool migrationEnabled = false,
                            string nameOrConnectionString = null,
                            string assemblyNameOrLocation = null,
                            Assembly configurationAssembly = null,
                            Type dbContextType = null,
                            bool mergeOnUpdate = false) 
        {
            _prefix = prefix;
            _lazyLoadingEnabled = lazyLoadingEnabled;
            _proxyCreationEnabled = proxyCreationEnabled;
            _autoDetectChangesEnabled = autoDetectChangesEnabled;
            _validateOnSaveEnabled = validateOnSaveEnabled;
            _migrationEnabled = migrationEnabled;
            _nameOrConnectionString = nameOrConnectionString;
            _configurationAssembly = configurationAssembly;
            if (_configurationAssembly != null)
            {
                _assemblyNameOrLocation = assemblyNameOrLocation;
            }
            _dbContextType = dbContextType;
            _mergeOnUpdate = mergeOnUpdate;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return Table<T>().Find(id);
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
            var idSelector = Expression.Lambda<Func<T, ID>>(body, parameter);

            var predicate = idSelector.BuildOrExpression<T, ID>(ids);

            return Table<T>().Where(predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Table<T>().FirstOrDefault(criteria);
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }
        
        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = Table<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = criteria.Apply(Table<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return PrepareSqlQuery<T>(command, parameters).ToArray();
        }

        protected DbRawSqlQuery<T> PrepareSqlQuery<T>(string command, ParamList parameters) where T : class
        {
            var connection = DbContext.Database.Connection;
            var query = parameters != null
                ? DbContext.Database.SqlQuery<T>(command, parameters.Select(p => DbFactory.CreateParameter(connection, p.Key, p.Value)).ToArray())
                : DbContext.Database.SqlQuery<T>(command);
            return query;
        }

        public T Add<T>(T entity) where T : class
        {
            return Table<T>().Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return Table<T>().Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var result = GetById<T, ID>(id);
            if (result != null)
            {
                Remove(result);
            }
            return result;
        }

        public T Update<T>(T entity) where T : class
        {
            var entry = DbContext.Entry(entity);
            if (entry == null)
            {
                return null;
            }

            var dbSet = Table<T>();
            if (entry.State == EntityState.Detached)
            {
                var comparer = new EntityEqualityComparer<T>(this);
                var hash = comparer.GetHashCode(entity);
                var attachedEntity = dbSet.Local.FirstOrDefault(e => comparer.GetHashCode(e) == hash);
                if (attachedEntity != null)
                {
                    // Update only root attributes for lazy loaded entities
                    if (DbContext.Configuration.LazyLoadingEnabled || !_mergeOnUpdate)
                    {
                        var attachedEntry = DbContext.Entry(attachedEntity);
                        attachedEntry.CurrentValues.SetValues(entity);
                        entry = attachedEntry;
                    }
                    else
                    {
                        Merge(entity, attachedEntity);
                        entry = DbContext.Entry(attachedEntity);
                    }
                }
                else
                {
                    dbSet.Attach(entity);
                    entry.State = EntityState.Modified;
                }
            }
            else
            {
                entry.State = EntityState.Modified;
            }

            return entry.Entity;
        }
        
        public void Attach<T>(T entity) where T : class
        {
            Table<T>().Attach(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            ((IObjectContextAdapter)DbContext).ObjectContext.Detach(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return Table<T>();
        }

        public long Count<T>() where T : class
        {
            return Table<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public DbSet<T> Table<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        protected DbContext DbContext
        {
            get
            {
                return ((IDataContext<DbContext>)DataContext).Session;
            }
        }

        public virtual IDataContext DataContext
        {
            get
            {
                return _context ?? (_context = new DataContext(_prefix, _lazyLoadingEnabled, _proxyCreationEnabled,
                    _autoDetectChangesEnabled, _validateOnSaveEnabled, _migrationEnabled, _nameOrConnectionString,
                    _assemblyNameOrLocation, _configurationAssembly, _dbContextType));
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
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return ((IObjectContextAdapter)this.DbContext)
                    .ObjectContext.CreateObjectSet<T>()
                    .EntitySet.ElementType.KeyMembers.Select(k => k.Name).ToArray();
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>();
            var values = new object[primaryKey.Length];
            for (var i = 0; i < primaryKey.Length; i++)
            {
                values[i] = PropertyAccessor.Get(entity, primaryKey[i]);
            }
            return values;
        }

        #endregion

        #region ILoadServiceProvider Members

        ILoadService<T> ILoadServiceProvider.Load<T>()
        {
            return new LoadService<T>(this);
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            readonly Repository _repository;
            IQueryable<T> _query;
            
            public LoadService(IRepository repository)
            {
                _repository = (Repository)repository;
                _query = repository.All<T>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) 
                where TProperty : class
            {
                _query = _query.Include(path);
                return this;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _query.FirstOrDefault(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                var query = _query.Where(criteria);
                return _repository.Page(query, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return Find(((Specification<T>)criteria).Predicate);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
            }

            public IQueryable<T> All()
            {
                return _query;
            }

            public T Update(T entity)
            {
                var loadedEntity = Find(_repository.As<IMetaDataProvider>().BuildPrimaryKeyExpression(entity));
                if (loadedEntity != null)
                {
                    _repository.Merge(entity, loadedEntity);
                }
                return loadedEntity;
            }

            public void Dispose()
            {
                
            }
        }

        #endregion

        #region Merge Methods

        private void Merge(object source, object target)
        {
            var context = DbContext;
            var comparer = new EntityEqualityComparer(this);
            var autoDetectChangesEnabled = context.Configuration.AutoDetectChangesEnabled;
            try
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                MergeImplementation(context, source, target, true, comparer, null);
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = autoDetectChangesEnabled;
            }
        }

        private static void MergeImplementation(DbContext context, object source, object target, bool root, IEqualityComparer<object> comparer, HashSet<Type> ancestors)
        {
            if (root)
            {
                context.Entry(target).CurrentValues.SetValues(source);
            }
            else if (target == null && source != null)
            {
                context.Set(source.GetType()).Add(source);
                return;
            }
            else if (target != null && source == null)
            {
                var entry = context.Entry(target);
                if (entry.State == EntityState.Unchanged)
                {
                    context.Set(target.GetType()).Remove(target);
                }
                return;
            }
            else if (target != null)
            {
                var entry = context.Entry(target);
                if (entry.State == EntityState.Detached)
                {
                    var hash = comparer.GetHashCode(target);
                    var attachedTarget = context.Set(target.GetType()).Local.Cast<object>().First(e => comparer.GetHashCode(e) == hash);
                    entry = context.Entry(attachedTarget);
                    entry.CurrentValues.SetValues(source);
                }
                else if (entry.State == EntityState.Unchanged)
                {
                    entry.CurrentValues.SetValues(source);
                }
            }
            else
            {
                return;
            }

            (ancestors = ancestors ?? new HashSet<Type>()).Add(source.GetType());

            var properties = source.GetType().GetProperties();
            
            foreach (var property in properties.Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsClass && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)))
            {
                if (ancestors.Contains(property.PropertyType))
                {
                    continue;
                }

                var value = PropertyAccessor.Get(target.GetType(), target, property.Name);
                var newValue = PropertyAccessor.Get(source.GetType(), source, property.Name);

                MergeImplementation(context, newValue, value, false, comparer, new HashSet<Type>(ancestors.Concat(new[] { property.PropertyType })));
            }

            foreach (var property in properties.Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)))
            {
                var propertyType = property.PropertyType.GetGenericArguments()[0];
                if (ancestors.Contains(propertyType))
                {
                    continue;
                }

                var values = (IEnumerable)PropertyAccessor.Get(target.GetType(), target, property.Name) ?? new object[] { };
                var newValues = (IEnumerable)PropertyAccessor.Get(source.GetType(), source, property.Name) ?? new object[] { };
                
                var updates = values.Cast<object>().Join(newValues.Cast<object>(), comparer.GetHashCode, comparer.GetHashCode, Tuple.Create).ToList();
                foreach (var item in updates)
                {
                    MergeImplementation(context, item.Item1, item.Item2, false, comparer, new HashSet<Type>(ancestors.Concat(new[] { propertyType })));
                }

                var deletes = values.Cast<object>().Except(newValues.Cast<object>(), comparer).ToList();
                foreach (var item in deletes)
                {
                    MergeImplementation(context, null, item, false, comparer, new HashSet<Type>(ancestors.Concat(new[] { propertyType })));
                }

                var inserts = newValues.Cast<object>().Except(values.Cast<object>(), comparer).ToList();
                foreach (var item in inserts)
                {
                    MergeImplementation(context, item, null, false, comparer, new HashSet<Type>(ancestors.Concat(new[] { propertyType })));
                }
            }
        }

        private class EntityEqualityComparer : IEqualityComparer<object>
        {
            private readonly Repository _repository;

            public EntityEqualityComparer(Repository repository)
            {
                _repository = repository;
            }

            private object[] GetPrimaryKey(object entity)
            {
                var method = typeof(IMetaDataProvider).GetMethod("GetPrimaryKeyValue").MakeGenericMethod(entity.GetType());
                return (object[])method.Invoke(_repository.As<IMetaDataProvider>(), new[] { entity });
            }
            
            public bool Equals(object x, object y)
            {
                return ArraysEqual(GetPrimaryKey(x), GetPrimaryKey(y));
            }

            public int GetHashCode(object obj)
            {
                return string.Join("::", GetPrimaryKey(obj).Select(v => v + "")).GetHashCode();
            }
        }

        private class EntityEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            private readonly Repository _repository;

            public EntityEqualityComparer(Repository repository)
            {
                _repository = repository;
            }

            private object[] GetPrimaryKey(T entity)
            {
                var primaryKey = _repository.As<IMetaDataProvider>().GetPrimaryKeyValue(entity);
                return primaryKey;
            }

            public bool Equals(T x, T y)
            {
                return ArraysEqual(GetPrimaryKey(x), GetPrimaryKey(y));
            }

            public int GetHashCode(T obj)
            {
                return string.Join("::", GetPrimaryKey(obj).Select(v => v + "")).GetHashCode();
            }
        }

        static bool ArraysEqual(object[] x, object[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            return !x.Where((t, i) => !t.Equals(y[i])).Any();
        }

        #endregion
    }
}
