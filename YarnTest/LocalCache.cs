using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Yarn.Cache;

namespace YarnTest
{
    public class LocalCache : ICachedResultProvider
    {
        private MemoryCache _cache = new MemoryCache("LocalCache");
        private CacheItemPolicy _policy;

        public LocalCache() : this(10) { }

        public LocalCache(int expiration)
        {
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(expiration) };
        }

        public T Get<T>(string key) where T : class
        {
            return (T)_cache.Get(key);
        }

        public bool Add<T>(string key, T value) where T : class
        {
            return _cache.Add(key, value, _policy);
        }

        public void Set<T>(string key, T value) where T : class
        {
            _cache.Set(key, value, _policy);
        }

        public T Remove<T>(string key) where T : class
        {
            return (T)_cache.Remove(key);
        }

        public int Evict(params string[] keys)
        {
            var count = 0;
            foreach (var key in keys)
            {
                if (_cache.Remove(key) != null)
                {
                    count++;
                }
            }
            return count;
        }

        public void Reset()
        {
            _cache.Trim(100);
        }

        public CacheItemPolicy CachePolicy
        {
            get
            {
                return _policy;
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
