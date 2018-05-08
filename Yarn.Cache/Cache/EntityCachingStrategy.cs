using System;

namespace Yarn.Cache
{
    public abstract class EntityCachingStrategy<T, TKey, TCache>
        where T : class
        where TCache : ICacheProvider
    {
        protected EntityCachingStrategy(IEntityRepository<T, TKey> repository, TCache cache)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
        }

        public abstract TCache Cache { get; }
    }
}