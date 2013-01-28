using System.Collections.Generic;
using Yarn;

namespace Yarn.Data
{
    public abstract class FullTextProvider : IFullTextProvider
    {
        public IDataContext DataContext { get; set; }
        public virtual void Index<T>() where T : class { }
        public virtual string Prepare<T>(string searchTerms) where T : class { return searchTerms; }
        public abstract IList<T> Search<T>(string searchTerms) where T : class;
    }
}
