using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using Yarn;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class FullTextRepository : Repository, IFullTextRepository
    {
        public FullTextRepository(IFullTextProvider fullTextProvider, IDataContext<DbContext> dataContext, RepositoryOptions options) : base(dataContext, options)
        {
            FullText = fullTextProvider;
        }

        public FullTextRepository(IFullTextProvider fullTextProvider, DataContextOptions dataContextOptions, RepositoryOptions options) : base(dataContextOptions, options)
        {
            FullText = fullTextProvider;
        }

        public IFullTextProvider FullText { get; }
    }
}
