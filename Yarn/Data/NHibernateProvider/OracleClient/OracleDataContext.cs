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
        public OracleDataContext() : this(null) { }

        public OracleDataContext(string contextKey = null)
            : base(OracleDataClientConfiguration.Oracle10, contextKey)
        { }

        protected override string DefaultFactoryKey
        {
            get
            {
                return "NHibernate.OracleClient";
            }
        }
    }
}
