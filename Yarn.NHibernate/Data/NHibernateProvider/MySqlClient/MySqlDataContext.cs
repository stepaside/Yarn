using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.MySqlClient
{
    public class MySqlDataContext : NHibernateDataContext<MySQLConfiguration, MySQLConnectionStringBuilder, MySQLDialect>
    {
        public MySqlDataContext() : this(null, null, null) { }

        public MySqlDataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public MySqlDataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public MySqlDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public MySqlDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public MySqlDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(MySQLConfiguration.Standard, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }
}
