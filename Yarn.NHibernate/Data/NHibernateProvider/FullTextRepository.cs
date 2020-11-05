using NHibernate;
using Yarn;

namespace Yarn.Data.NHibernateProvider
{
    public class FullTextRepository : Repository, IFullTextRepository
    {
        public FullTextRepository(IDataContext<ISession> context, IFullTextProvider fullTextProvider)
            : base(context)
        {
            FullText = fullTextProvider;
        }

        public IFullTextProvider FullText
        {
            get;
        }
    }
}
