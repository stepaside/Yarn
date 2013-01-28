using NHibernate.Dialect;
using NHibernate.Dialect.Function;

namespace Yarn.Data.NHibernateProvider.SqlClient.Dialects
{
    public class MsSql2008DialectWithFullTextSupport : MsSql2008Dialect
    {
        public MsSql2008DialectWithFullTextSupport()
        {
            RegisterFunction("freetext", new FreeTextFunction());
            RegisterFunction("freetexttable", new StandardSQLFunction("freetexttable"));
            RegisterFunction("contains", new ContainsFunction());
            RegisterFunction("containstable", new StandardSQLFunction("containstable"));
        }
    }
}
