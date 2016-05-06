using NDatabase.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NDatabase.Api.Query;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.InMemoryProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider, IBulkOperationsProvider
    {
        private DataContext _context;
        private readonly IMetaDataProvider _metaDataProvider;

        public Repository(IMetaDataProvider metaDataProvider = null)
        {
            _metaDataProvider = metaDataProvider ?? this;
            _context = new DataContext();
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            if (typeof(OID).IsAssignableFrom(typeof(TKey)))
            {
                return (T)_context.Session.GetObjectFromId((OID)id);
            }
            
            var predicate = _metaDataProvider.BuildPrimaryKeyExpression<T, TKey>(id);
            return All<T>().FirstOrDefault(predicate);
        }
        
        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = All<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return new List<T>();
        }

        public T Add<T>(T entity) where T : class
        {
            _context.Session.Store(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            _context.Session.Delete(entity);
            return entity;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var oid = id as OID;
            if (oid != null)
            {
                var entity = (T)_context.Session.GetObjectFromId(oid);
                _context.Session.DeleteObjectWithId(oid);
                return entity;
            }
            else
            {
                var entity = GetById<T, TKey>(id);
                if (entity != null)
                {
                    Remove(entity);
                }
                return entity;
            }
        }

        public T Update<T>(T entity) where T : class
        {
             _context.Session.Store(entity);
             return entity;
        }

        public long Count<T>() where T : class
        {
            return All<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return All<T>().LongCount(criteria);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _context.Session.AsQueryable<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            Remove(entity);
        }

        public void Attach<T>(T entity) where T : class
        {
            Update(entity); 
        }

        protected IOdb Database
        {
            get
            {
                return ((IDataContext<IOdb>)DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get { return _context ?? (_context = new DataContext()); }
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
                _context.Dispose();
                _context = null;
            }
        }

        protected OID GetId<T>(T entity) where T : class
        {
            return Database.GetObjectId(entity);
        }

        #region IMetaDataProvider Members

        private static bool IsPrimaryKey(PropertyInfo p)
        {
            var attr = p.GetCustomAttributes(false).OfType<OIDAttribute>().ToList();
            if (attr.Count == 0)
            {
                return typeof(OID).IsAssignableFrom(p.PropertyType) || string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase) || (p.ReflectedType != null && string.Equals(p.Name, p.ReflectedType.Name + "id", StringComparison.OrdinalIgnoreCase));
            }
            return true;
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return typeof(T).GetProperties().Where(IsPrimaryKey).Select(p => p.Name).ToArray();
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>();
            var values = new object[primaryKey.Length];
            for (var i = 0; i < primaryKey.Length; i++ )
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
            private readonly Repository _repository;

            public LoadService(Repository repository)
            {
                _repository = repository;
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path)
                where TProperty : class
            {
                return this;
            }

            public IQueryable<T> All()
            {
                return _repository.All<T>();
            }

            public void Dispose()
            {
                
            }

            public T Find(ISpecification<T> criteria)
            {
                return _repository.Find(criteria);
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _repository.Find(criteria);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return _repository.FindAll(criteria, offset, limit, orderBy);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return _repository.FindAll(criteria, offset, limit, orderBy);
            }
            
            public T Update(T entity)
            {
                return _repository.Update(entity);
            }
        }

        #endregion

        #region IBulkOperationsProvider Members

        IEnumerable<T> IBulkOperationsProvider.GetById<T, TKey>(IEnumerable<TKey> ids)
        {
            if (typeof(OID).IsAssignableFrom(typeof(TKey)))
            {
                return ids.Select(id => (T)_context.Session.GetObjectFromId((OID)id));
            }

            var primaryKey = _metaDataProvider.GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(TKey));
            var idSelector = Expression.Lambda<Func<T, TKey>>(body, parameter);

            var predicate = idSelector.BuildOrExpression(ids.ToList());

            return All<T>().Where(predicate).AsEnumerable();
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            var count = entities.Select(entity => _context.Session.Store(entity)).LongCount(id => id != null && id.ObjectId > 0);
            _context.SaveChanges();
            return count;
        }

        public long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class
        {
            throw new NotImplementedException();
        }

        public long Update<T>(params BulkUpdateOperation<T>[] bulkOperations) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            var count = entities.Select(entity => _context.Session.Delete(entity)).LongCount(id => id != null && id.ObjectId > 0);
            _context.SaveChanges();
            return count;
        }

        public long Delete<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            var entities = ((IBulkOperationsProvider)this).GetById<T, TKey>(ids);
            return Delete(entities);
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            var total = criteria.Sum(predicate => _context.Session.AsQueryable<T>().Where(predicate).Select(entity => _context.Session.Delete(entity)).LongCount(id => id != null && id.ObjectId > 0));
            _context.SaveChanges();
            return total;
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(c => ((Specification<T>)c).Predicate).ToArray());
        }
        
        #endregion
    }
}
