using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace Yarn.Cache
{
    public static class CacheExtensions
    {
        public static Repository<TCache> WithCache<TCache>(this IRepository repository, TCache cache = null)
            where TCache : class, ICachedResultProvider, new()
        {
            return new Repository<TCache>(repository, cache);
        }
    }
}
