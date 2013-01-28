using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yarn;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class FullTextRepository : Repository
    {
        private FullTextProvider _fullTextProvider;

        public FullTextRepository(string contextKey = null)
            : base(contextKey)
        { }

        public FullTextProvider FullText
        {
            get
            {
                if (_fullTextProvider == null)
                {
                    _fullTextProvider = ObjectFactory.Resolve<FullTextProvider>(_contextKey);
                    _fullTextProvider.DataContext = this.DataContext;
                }
                return _fullTextProvider;
            }
        }
    }
}
