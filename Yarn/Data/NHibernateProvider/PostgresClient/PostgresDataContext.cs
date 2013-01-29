using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.OracleClient
{
    public class PostgresDataContext : NHibernateDataContext<PostgreSQLConfiguration, PostgreSQLConnectionStringBuilder, PostgreSQLDialect>
    {
        public PostgresDataContext() : this(null) { }

        public PostgresDataContext(string contextKey = null)
            : base(PostgreSQLConfiguration.Standard, contextKey)
        { }

        protected override string DefaultFactoryKey
        {
            get
            {
                return "NHibernate.PostgresClient";
            }
        }
    }
}
