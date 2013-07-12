using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn.Specification;
using System.Collections.Concurrent;

namespace Yarn.Cache
{
    public class CachedRepository<TCache> : IRepository
        where TCache : class, ICachedResultProvider, new()
    {
        private static ConcurrentDictionary<Type, HashSet<string>> _queries = new ConcurrentDictionary<Type, HashSet<string>>();

        private IRepository _repository;
        private TCache _cache;
        HashSet<Type> _types;

        public CachedRepository(IRepository repository, TCache cache = null)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
            _cache = cache ?? new TCache();
            _types = new HashSet<Type>();
        }

        #region IRepository Members

        public T GetById<T, ID>(ID id) where T : class
        {
            var key = typeof(T).FullName + ".GetById(id:" + id.ToString() + ")";
            var item = _cache.Get<T>(key);
            if (item == null)
            {
                item = _repository.GetById<T, ID>(id);
                _cache.Set<T>(key, item);
                RecordQuery<T>(key);
            }
            return item;
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var key = typeof(T).FullName + ".GetByIdList(ids:[" + string.Join(",", ids.OrderBy(_=>_)) + "])";
            var items = _cache.Get<IList<T>>(key);
            if (items == null)
            {
                items = _repository.GetByIdList<T, ID>(ids).ToArray();
                _cache.Set<IList<T>>(key, items);
                RecordQuery<T>(key);
            }
            return items;
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find<T>(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            var key = typeof(T).FullName + ".Find(criteria:" + criteria.ToString() + ")";
            var item = _cache.Get<T>(key);
            if (item == null)
            {
                item = _repository.Find<T>(criteria);
                _cache.Set<T>(key, item);
                RecordQuery<T>(key);
            }
            return item;
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            return FindAll<T>(((Specification<T>)criteria).Predicate, offset, limit);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            // Reduce invalid cache combinations
            if (offset < 0) offset = 0;
            if (limit < 0) limit = 0;

            var key = typeof(T).FullName + ".FindAll(criteria:" + criteria.ToString() + ",offset:" + offset + ",limit:" + limit + ")";
            var items = _cache.Get<IList<T>>(key);
            if (items == null)
            {
                items = _repository.FindAll<T>(criteria, offset, limit).ToArray();
                _cache.Set<IList<T>>(key, items);
                RecordQuery<T>(key);
            }
            return items;
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            var key = typeof(T).FullName + ".Execute(command:" + command + (parameters != null ? "," + string.Join(",", parameters.OrderBy(p => p.Key).Select(p => p.Key + ":" + p.Value)) : "") + ")";
            var items = _cache.Get<IList<T>>(key);
            if (items == null)
            {
                items = _repository.Execute<T>(command, parameters);
                _cache.Set<IList<T>>(key, items);
                RecordQuery<T>(key);
            }
            return items;
        }

        public T Add<T>(T entity) where T : class
        {
            _types.Add(typeof(T));
            return _repository.Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            _types.Add(typeof(T));
            return _repository.Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            _types.Add(typeof(T));
            return _repository.Remove<T, ID>(id);
        }

        public T Merge<T>(T entity) where T : class
        {
            _types.Add(typeof(T));
            return _repository.Merge(entity);
        }

        public long Count<T>() where T : class
        {
            return _repository.Count<T>();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public IQueryable<T> All<T>() where T : class
        {
            return _repository.All<T>();
        }

        public void Detach<T>(T entity) where T : class
        {
            _repository.Detach(entity);
        }

        public void Attach<T>(T entity) where T : class
        {
            _repository.Attach(entity);
        }

        public void SaveChanges()
        {
            _repository.SaveChanges();
            foreach (var type in _types)
            {
                EvictQueries(type);
            }
        }

        public IDataContext DataContext
        {
            get 
            { 
                return _repository.DataContext; 
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repository.Dispose();
                _cache.Dispose();
            }
        }

        public bool EvictQueries(Type type)
        {
            HashSet<string> queries;
            if (_queries.TryRemove(type, out queries))
            {
                _cache.Clear(queries.ToArray());
                return true;
            }
            return false;
        }

        private void RecordQuery<T>(string query) where T : class
        {
            var queries = _queries.GetOrAdd(typeof(T), t => new HashSet<string>());
            queries.Add(query);
        }
    }
}
