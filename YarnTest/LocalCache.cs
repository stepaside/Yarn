using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Yarn.Cache;

namespace Yarn.Test
{
    public class LocalCache : ICacheProvider
    {
        private MemoryCache _cache = new MemoryCache("LocalCache");
        private CacheItemPolicy _policy;
        private static readonly object Locker = new object();

        public LocalCache() : this(10) { }

        public LocalCache(int expiration)
        {
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(expiration) };
        }

        public bool Get<T>(string key, out T item)
        {
            item = default(T);
            try
            {
                if (!Exists(key))
                {
                    return false;
                }
                item = (T)_cache.Get(key);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool Add<T>(string key, T value)
        {
            return _cache.Add(key, value, _policy);
        }

        public void Set<T>(string key, T value)
        {
            _cache.Set(key, value, _policy);
        }

        public bool Remove(string key)
        {
            return _cache.Remove(key) != null;
        }
        
        public void Reset()
        {
            _cache.Trim(100);
        }

        public bool Exists(string key)
        {
            return _cache.Contains(key);
        }

        public uint Increment(string key, uint initial = 1, uint delta = 1)
        {
            lock (Locker)
            {
                uint current;
                if (!Get(key, out current))
                {
                    current = initial;
                }

                var newValue = current + delta;
                Set(key, newValue);
                return newValue;
            }
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
