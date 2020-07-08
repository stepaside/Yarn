using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.OracleClient
{
    public class OracleDataContext : NHibernateDataContext<OracleDataClientConfiguration, OracleConnectionStringBuilder, Oracle10gDialect>
    {
        public OracleDataContext() : this(null, null, null) { }

        public OracleDataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public OracleDataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public OracleDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public OracleDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public OracleDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(OracleDataClientConfiguration.Oracle10, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }
}
