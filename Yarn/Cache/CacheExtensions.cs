using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn.Cache
{
    public static class CacheExtensions
    {
        public static CachedRepository<TCache> UseCache<TCache>(this IRepository repository)
            where TCache : class, ICachedResultProvider, new()
        {
            return new CachedRepository<TCache>(repository);
        }
    }
}
