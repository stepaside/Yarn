using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.OracleClient
{
    public class PostgresDataContext : NHibernateDataContext<PostgreSQLConfiguration, PostgreSQLConnectionStringBuilder, PostgreSQLDialect>
    {
        public PostgresDataContext() : this(null, null, null) { }

        public PostgresDataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public PostgresDataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public PostgresDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public PostgresDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public PostgresDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(PostgreSQLConfiguration.Standard, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }
}
