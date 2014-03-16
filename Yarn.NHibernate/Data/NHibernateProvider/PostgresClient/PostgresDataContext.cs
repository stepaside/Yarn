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
        public PostgresDataContext() : this(null, null) { }

        public PostgresDataContext(string nameOrConnectionString = null) : this(null, nameOrConnectionString) { }

        public PostgresDataContext(string prefix = null, string nameOrConnectionString = null)
            : base(PostgreSQLConfiguration.Standard, prefix, nameOrConnectionString)
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
