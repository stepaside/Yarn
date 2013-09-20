using NDatabase.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Data.InMemoryProvider
{
    public class Repository : IRepository
    {
        private DataContext _context = null;
        private IMetaDataProvider _metaDataProvider = null;

        public Repository(IMetaDataProvider metaDataProvider)
        {
            _metaDataProvider = metaDataProvider;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return GetByIdList<T, ID>(new[] { id }).FirstOrDefault();
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
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

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            return FindAll<T>(((Specification<T>)criteria).Predicate, offset, limit);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            var query = this.All<T>().Where(criteria).Skip(offset);
            if (limit > 0)
            {
                query = query.Take(limit);
            }
            return query.AsEnumerable();
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

        public T Merge<T>(T entity) where T : class
        {
             _context.Session.Store<T>(entity);
             return entity;
        }

        public long Count<T>() where T : class
        {
            throw new NotImplementedException();
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
        { }

        public void Attach<T>(T entity) where T : class
        { }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public IDataContext<IOdb> PrivateContext
        {
            get
            {
                return (IDataContext<IOdb>)this.DataContext;
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
            _context = null;
        }
    }
}
