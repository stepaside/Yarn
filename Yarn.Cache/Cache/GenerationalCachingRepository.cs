using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn.Specification;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Yarn.Cache
{
    public class GenerationalCachingRepository<TCache> : CachingStrategy<TCache>, IRepository
        where TCache : class, ICacheProvider, new()
    {
        private IMetaDataProvider _metaData;
        private IRepository _repository;
        private IDataContext _context;

        private TCache _cache;
        private List<Action> _delayedCache;

        public GenerationalCachingRepository(IRepository repository, TCache cache = null) 
            : base(repository, cache)
        {
            _repository = repository;
            _metaData = (IMetaDataProvider)repository;
            _context = new DataContext(_repository.DataContext, OnAfterCommit);
            _cache = cache ?? new TCache();
            _delayedCache = new List<Action>();
        }

        #region CachingStrategy Members

        public override TCache Cache
        {
            get { return _cache; }
        }

        #endregion

        #region IRepository Members

        public T GetById<T, ID>(ID id) where T : class
        {
            var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId<ID>(id) } }, false);
            T item = null;
            if (_cache.Get<T>(key, out item))
            {
                return item;
            }

            item = _repository.GetById<T, ID>(id);
            SetWriteThroughCache<T>(key, item);

            return item;
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var items = new List<T>();
            var missingIds = new Dictionary<string, ID>();
            foreach (var id in ids)
            {
                var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId<ID>(id) } }, false);

                T item = null;
                var cached = _cache.Get<T>(key, out item);

                if (cached) 
                {
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
                else
                {
                    missingIds[key] = id;
                }
            }

            if (missingIds.Count > 0)
            {
                foreach (var missingId in missingIds)
                {
                    var item = _repository.GetById<T, ID>(missingId.Value);
                    if (item != null)
                    {
                        items.Add(item);
                        SetWriteThroughCache<T>(missingId.Key, item);
                    }
                }
            }

            return items;
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find<T>(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria, limit: 1).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return FindAll<T>(((Specification<T>)criteria).Predicate, offset, limit);
        }

        public IEnumerable<T> FindAll<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            // Reduce invalid cache combinations
            if (offset < 0) offset = 0;
            if (limit < 0) limit = 0;

            var key = CacheKey<T>(criteria, offset, limit, orderBy);
            IList<T> items;
            if (!_cache.Get<IList<T>>(key, out items))
            {
                items = _repository.FindAll<T>(criteria, offset, limit).ToArray();
                _cache.Set<IList<T>>(key, items);
            }

            return items;
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            IEnumerable<dynamic> values = new[] { new { Name = "command", Value = command } };

            if (parameters != null)
            {
                values = values.Concat(parameters.OrderBy(p => p.Key).Select(p => new { Name = p.Key, Value = p.Value + "" }));
            }

            var key = CacheKey<T>("Execute", values, true);
            IList<T> items;
            if (_cache.Get<IList<T>>(key, out items))
            {
                items = _repository.Execute<T>(command, parameters);
                _cache.Set<IList<T>>(key, items);
            }

            return items;
        }

        public T Add<T>(T entity) where T : class
        {
            try
            {
                return _repository.Add(entity);
            }
            finally
            {
                var id = _metaData.GetPrimaryKeyValue<T>(entity);
                var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
                _delayedCache.Add(() =>
                {
                    SetWriteThroughCache<T>(key, entity);
                    NextGeneration<T>();
                });
            }
        }

        public T Remove<T>(T entity) where T : class
        {
            try
            {
                return _repository.Remove(entity);
            }
            finally
            {
                var id = _metaData.GetPrimaryKeyValue<T>(entity);
                var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
                _delayedCache.Add(() =>
                {
                    _cache.Remove(key);
                    NextGeneration<T>();
                });
            }
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            try
            {
                return _repository.Remove<T, ID>(id);
            }
            finally
            {
                var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId<ID>(id) } }, false);
                _delayedCache.Add(() =>
                {
                    _cache.Remove(key);
                    NextGeneration<T>();
                });
            }
        }

        public T Update<T>(T entity) where T : class
        {
            try
            {
                return _repository.Update(entity);
            }
            finally
            {
                var id = _metaData.GetPrimaryKeyValue<T>(entity);
                var key = CacheKey<T>("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
                _delayedCache.Add(() =>
                {
                    SetWriteThroughCache<T>(key, entity);
                    NextGeneration<T>();
                });
            }
        }

        public long Count<T>() where T : class
        {
            return _repository.All<T>().LongCount();
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

        public IDataContext DataContext
        {
            get
            {
                return _context;
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
                _cache.Dispose();
            }
        }

        private void OnAfterCommit()
        {
            foreach (var action in _delayedCache)
            {
                action();
            }
            _delayedCache.Clear();
        }

        private string ConvertId<ID>(ID id)
        {
            if (id is object[])
            {
                return string.Join("|", ((object[])(object)id).Select(i => i.ToString()));
            }
            else
            {
                return id.ToString();
            }
        }

        private void SetWriteThroughCache<T>(string key, T item)
        {
            _cache.Set<T>(key, item);
        }

        private uint GetGeneration<T>()
        {
            uint generation;
            return !_cache.Get(GenerationKey<T>(), out generation) ? 1 : generation;
        }

        private uint NextGeneration<T>()
        {
            return _cache.Increment(GenerationKey<T>(), 1, 1);
        }

        private string GenerationKey<T>()
        {
            return string.Format("{0}/Generation", typeof(T).FullName);
        }

        private string CacheKey<T>(string operation, IEnumerable<dynamic> parameters, bool query)
        {
            var parametersValue = parameters != null ? "/" + string.Join(",", parameters.Select(t => t.Name + "=" + t.Value)) : "";
            if (query)
            {
                return string.Concat(typeof(T).FullName, "/", GetGeneration<T>(), "/", operation, parametersValue);
            }
            else
            {
                return string.Concat(typeof(T).FullName, "/", operation, parametersValue);
            }
        }

        private string CacheKey<T>(Expression<Func<T, bool>> predicate = null, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
        {
            var predicateKey = predicate == null ? "All" : string.Concat(predicate.ToString(), ",", offset, ",", limit);
            if (orderBy != null)
            {
                predicateKey += "/" + orderBy.ToString();
            }
            return string.Concat(typeof(T).FullName, "/", GetGeneration<T>(), "/", predicateKey);
        }
    }
}
