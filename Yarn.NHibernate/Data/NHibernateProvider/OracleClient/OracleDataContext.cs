using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.OracleClient
{
    public class OracleDataContext : NHibernateDataContext<OracleDataClientConfiguration, OracleConnectionStringBuilder, Oracle10gDialect>
    {
        public OracleDataContext() : this(null, null) { }

        public OracleDataContext(string nameOrConnectionString = null) : this(null, nameOrConnectionString) { }

        public OracleDataContext(string prefix = null, string nameOrConnectionString = null)
            : base(OracleDataClientConfiguration.Oracle10, prefix, nameOrConnectionString)
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
