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
        public PostgresDataContext() : this(null, null, null, null) { }

        public PostgresDataContext(string assemblyNameOrLocation = null) : this(null, null, assemblyNameOrLocation, null) { }

        public PostgresDataContext(Assembly configurationAssembly = null) : this(null, null, null, configurationAssembly) { }

        public PostgresDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(null, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public PostgresDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(null, nameOrConnectionString, null, configurationAssembly) { }

        public PostgresDataContext(string prefix = null, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(PostgreSQLConfiguration.Standard, prefix, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }

        protected override string DefaultPrefix
        {
            get
            {
                return "NHibernate.PostgresClient";
            }
        }
    }
}
