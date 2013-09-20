using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class FullTextRepository : Repository, IFullTextRepository
    {
        private IFullTextProvider _fullTextProvider;

        public FullTextRepository()
            : this(null)
        { }

        public FullTextRepository(string contextKey = null)
            : base(contextKey)
        { }

        public IFullTextProvider FullText
        {
            get
            {
                if (_fullTextProvider == null)
                {
                    _fullTextProvider = ObjectFactory.Resolve<IFullTextProvider>(_prefix);
                    _fullTextProvider.DataContext = this.DataContext;
                }
                return _fullTextProvider;
            }
        }
    }
}
