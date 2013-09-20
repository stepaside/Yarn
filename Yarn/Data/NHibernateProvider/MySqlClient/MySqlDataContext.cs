using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.MySqlClient
{
    public class MySqlDataContext : NHibernateDataContext<MySQLConfiguration, MySQLConnectionStringBuilder, MySQLDialect>
    {
        public MySqlDataContext() : this(null) { }
        
        public MySqlDataContext(string contextKey = null)
            : base(MySQLConfiguration.Standard, contextKey)
        { }

        protected override string DefaultPrefix
        {
            get
            {
                return "NHibernate.MySqlClient";
            }
        }
    }
}
