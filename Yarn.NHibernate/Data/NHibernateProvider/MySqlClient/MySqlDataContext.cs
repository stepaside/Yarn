using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.MySqlClient
{
    public class MySqlDataContext : NHibernateDataContext<MySQLConfiguration, MySQLConnectionStringBuilder, MySQLDialect>
    {
        public MySqlDataContext() : this(null, null, null, null) { }

        public MySqlDataContext(string assemblyNameOrLocation = null) : this(null, null, assemblyNameOrLocation, null) { }

        public MySqlDataContext(Assembly configurationAssembly = null) : this(null, null, null, configurationAssembly) { }

        public MySqlDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(null, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public MySqlDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(null, nameOrConnectionString, null, configurationAssembly) { }

        public MySqlDataContext(string prefix = null, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(MySQLConfiguration.Standard, prefix, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
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
