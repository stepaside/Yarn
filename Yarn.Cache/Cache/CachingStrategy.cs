using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Cache
{
    public abstract class CachingStrategy<TCache>
        where TCache : ICacheProvider
    {
        protected CachingStrategy(IRepository repository, TCache cache)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
        }

        public abstract TCache Cache { get; }
    }
}
