using Yarn.Data.NHibernateProvider.SqlClient.Dialects;
using FluentNHibernate.Cfg.Db;

namespace Yarn.Data.NHibernateProvider.SqlClient
{
    public class SqlDataContext : NHibernateDataContext<MsSqlConfiguration, MsSqlConnectionStringBuilder, MsSql2008DialectWithFullTextSupport>
    {
        private static NHibernate.Cfg.Configuration _config = null;

        public SqlDataContext() : this(null) { }

        public SqlDataContext(string contextKey = null)
            : base(MsSqlConfiguration.MsSql2008, contextKey)
        { }

        protected override string DefaultFactoryKey
        {
            get
            {
                return "NHibernate.SqlClient";
            }
        }
    }
}
