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
        public OracleDataContext() : this(null, null, null, null) { }

        public OracleDataContext(string assemblyNameOrLocation = null) : this(null, null, assemblyNameOrLocation, null) { }

        public OracleDataContext(Assembly configurationAssembly = null) : this(null, null, null, configurationAssembly) { }

        public OracleDataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(null, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public OracleDataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(null, nameOrConnectionString, null, configurationAssembly) { }

        public OracleDataContext(string prefix = null, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(OracleDataClientConfiguration.Oracle10, prefix, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }

        protected override string DefaultPrefix
        {
            get
            {
                return "NHibernate.OracleClient";
            }
        }
    }
}
