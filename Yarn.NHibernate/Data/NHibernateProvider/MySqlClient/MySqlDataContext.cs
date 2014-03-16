using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.MySqlClient
{
    public class MySqlDataContext : NHibernateDataContext<MySQLConfiguration, MySQLConnectionStringBuilder, MySQLDialect>
    {
        public MySqlDataContext() : this(null, null) { }

        public MySqlDataContext(string nameOrConnectionString = null) : this(null, nameOrConnectionString) { }

        public MySqlDataContext(string prefix = null, string nameOrConnectionString = null)
            : base(MySQLConfiguration.Standard, prefix, nameOrConnectionString)
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
