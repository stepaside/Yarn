using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Yarn.Adapters;
using Yarn.Extensions;
using Yarn.Linq.Expressions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Cache
{
    public class GenerationalEntityCachingRepository<T, TKey, TCache> : EntityCachingStrategy<T, TKey, TCache>, IEntityRepository<T, TKey>
        where T : class
        where TCache : class, ICacheProvider, new()
    {
        private readonly IEntityRepository<T, TKey> _repository;
        private readonly Func<T, TKey> _keySelector;

        private readonly TCache _cache;
        private readonly List<Action> _delayedCache;

        private static readonly IQueryable<T> EmptyQueryable = new T[] { }.AsQueryable();

        public GenerationalEntityCachingRepository(IEntityRepository<T, TKey> repository, Func<T, TKey> keySelector, TCache cache = null) 
            : base(repository, cache)
        {
            _repository = repository;
            _keySelector = keySelector;
            _cache = cache ?? new TCache();
            _delayedCache = new List<Action>();
        }

        #region CachingStrategy Members

        public override TCache Cache
        {
            get { return _cache; }
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

        private static string ConvertId(TKey id)
        {
            var list = id as object[];
            return list != null ? string.Join("|", list.Select(i => i.ToString())) : id.ToString();
        }

        private void SetWriteThroughCache(string key, T item)
        {
            _cache.Set(key, item);
        }

        private uint GetGeneration()
        {
            return !_cache.Get(GenerationKey(), out uint generation) ? 1 : generation;
        }

        private uint NextGeneration()
        {
            return _cache.Increment(GenerationKey(), 1, 1);
        }

        private static string GenerationKey()
        {
            return $"{typeof(T).FullName}/Generation";
        }

        private string CacheKey(string operation, IEnumerable<dynamic> parameters, bool query)
        {
            var parametersValue = parameters != null ? "/" + ComputeHash(string.Join(",", parameters.Select(t => t.Name + "=" + t.Value))) : "";
            
            return query ? string.Concat(typeof(T).FullName, "/", GetGeneration(), "/", operation, parametersValue) : string.Concat(typeof(T).FullName, "/", operation, parametersValue);
        }

        private string CacheKey(Expression<Func<T, bool>> predicate = null, PagedSpecification<T> paging = null)
        {
            var identity = new StringBuilder();

            if (predicate != null)
            {
                Expression expression = predicate;

                // locally evaluate as much of the query as possible
                expression = Evaluator.PartialEval(expression);
                
                // support local collections
                expression = LocalCollectionExpander.Rewrite(expression);
                identity.Append(expression);
            }
            else
            {
                identity.Append("All");
            }

            if (paging?.PageNumber > 0)
            {
                identity.AppendFormat("|page:{0}", paging.PageNumber);
            }

            if (paging?.PageSize > 0)
            {
                identity.AppendFormat("|size:{0}", paging.PageSize);
            }

            if (paging?.Sorting != null)
            {
                identity.AppendFormat("|orderby:{0}", paging.Sorting);
            }

            var hash = ComputeHash(identity.ToString());

            return string.Concat(typeof(T).FullName, "/", GetGeneration(), "/", hash);
        }

        //djb2 hash
        private static uint ComputeHash(string value)
        {
            unsafe
            {
                fixed (char* start = value)
                {
                    uint hash = 5381; ;
                    char* ch = start;
                    uint c;
                    while ((c = ch[0]) != 0)
                    {
                        hash = ((hash << 5) + hash) ^ c;
                        c = ch[1];
                        if (c == 0)
                        {
                            break;
                        }
                        hash = ((hash << 5) + hash) ^ c;
                        ch += 2;
                    }
                    return hash;
                }
            }
        }

        public IQueryResult<T> GetAll()
        {
            return _repository.GetAll();
        }

        public T GetById(TKey id)
        {
            var key = CacheKey("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
            if (_cache.Get(key, out T item))
            {
                return item;
            }

            item = _repository.GetById(id);
            SetWriteThroughCache(key, item);

            return item;
        }

        public IQueryResult<T> Find(ISpecification<T> criteria)
        {
            var paging = criteria as PagedSpecification<T>;
            var query = criteria.Apply(EmptyQueryable).Expression as Expression<Func<T, bool>>;

            var key = CacheKey(query, paging);
            if (!_cache.Get(key, out IQueryResult<T> items))
            {
                items = _repository.Find(criteria);
                _cache.Set(key, items);
            }

            return items;
        }

        public bool Save(T entity)
        {
            try
            {
                return _repository.Save(entity);
            }
            finally
            {
                var id = _keySelector(entity);
                var key = CacheKey("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
                _delayedCache.Add(() =>
                {
                    SetWriteThroughCache(key, entity);
                    NextGeneration();
                });
            }
        }

        public void Remove(T entity)
        {
            Remove(_keySelector(entity));
        }

        public T Remove(TKey id)
        {
            try
            {
                return _repository.Remove(id);
            }
            finally
            {
                var key = CacheKey("GetById", new[] { new { Name = "id", Value = ConvertId(id) } }, false);
                _delayedCache.Add(() =>
                {
                    _cache.Remove(key);
                    NextGeneration();
                });
            }
        }
    }
}
