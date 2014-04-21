using NDatabase.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.InMemoryProvider
{
    public class Repository : IRepository, IMetaDataProvider
    {
        private DataContext _context = null;
        private IMetaDataProvider _metaDataProvider = null;

        public Repository(IMetaDataProvider metaDataProvider = null)
        {
            _metaDataProvider = metaDataProvider ?? this;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return GetById<T, ID>(new[] { id }).FirstOrDefault();
        }

        public IEnumerable<T> GetById<T, ID>(IList<ID> ids) where T : class
        {
            if (typeof(OID).IsAssignableFrom(typeof(T)))
            {
                return ids.Select(id => (T)_context.Session.GetObjectFromId((OID)id));
            }
            else
            {
                var primaryKey = _metaDataProvider.GetPrimaryKey<T>().First();

                var parameter = Expression.Parameter(typeof(T));
                var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
                var idSelector = Expression.Lambda<Func<T, ID>>(body, parameter);

                var predicate = idSelector.BuildOrExpression<T, ID>(ids);

                return this.All<T>().Where(predicate).AsEnumerable();
            }
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).FirstOrDefault();
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return FindAll<T>(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = this.All<T>().Where(criteria);
            return this.Page<T>(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return new List<T>();
        }

        public T Add<T>(T entity) where T : class
        {
            _context.Session.Store<T>(entity);
            return entity;
        }

        public T Remove<T>(T entity) where T : class
        {
            _context.Session.Delete(entity);
            return entity;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            if (id is OID)
            {
                var entity = (T)_context.Session.GetObjectFromId((OID)id);
                _context.Session.DeleteObjectWithId((OID)id);
                return entity;
            }
            else
            {
                var entity = GetById<T, ID>(id);
                if (entity != null)
                {
                    Remove(entity);
                }
                return entity;
            }
        }

        public T Update<T>(T entity) where T : class
        {
             _context.Session.Store<T>(entity);
             return entity;
        }

        public long Count<T>() where T : class
        {
            return this.All<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return this.All<T>().LongCount(criteria);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _context.Session.AsQueryable<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            Remove<T>(entity);
        }

        public void Attach<T>(T entity) where T : class
        {
            Update<T>(entity); 
        }

        protected IOdb Database
        {
            get
            {
                return ((IDataContext<IOdb>)this.DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new DataContext();
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
                _context.Dispose();
                _context = null;
            }
        }

        protected OID GetId<T>(T entity) where T : class
        {
            return this.Database.GetObjectId<T>(entity);
        }

        #region IMetaDataProvider Members

        private static bool IsPrimaryKey(PropertyInfo p)
        {
            var attr = p.GetCustomAttributes(false).OfType<OIDAttribute>().ToList();
            if (attr.Count == 0)
            {
                return typeof(OID).IsAssignableFrom(p.PropertyType) || string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return true;
            }
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return typeof(T).GetProperties().Where(p => IsPrimaryKey(p)).Select(p => p.Name).ToArray();
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
    }
}
