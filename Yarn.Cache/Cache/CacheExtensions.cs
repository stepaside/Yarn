using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace Yarn.Cache
{
    public static class CacheExtensions
    {
        public static IRepository WithCache<TCache>(this IRepository repository, TCache cache = null)
            where TCache : class, ICacheProvider, new()
        {
            return new GenerationalCachingRepository<TCache>(repository, cache);
        }

        public static IEntityRepository<T, TKey> WithCache<T, TKey, TCache>(this IEntityRepository<T, TKey> repository, IEntityKeySelector<T, TKey> keySelector, TCache cache = null)
            where T : class
            where TCache : class, ICacheProvider, new()
        {
            return new GenerationalEntityCachingRepository<T, TKey, TCache>(repository, keySelector.GetKey, cache);
        }
    }
}
