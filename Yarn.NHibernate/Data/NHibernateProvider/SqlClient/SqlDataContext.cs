using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.SqlClient
{
    public class SqlDataContext<TDialect> : NHibernateDataContext<MsSqlConfiguration, MsSqlConnectionStringBuilder, TDialect>
        where TDialect : MsSql2000Dialect
    {
        public SqlDataContext(MsSqlConfiguration configuration) : this(configuration, null) { }

        public SqlDataContext(MsSqlConfiguration configuration, string contextKey = null)
            : base(configuration, contextKey)
        { }

        protected override string DefaultPrefix
        {
            get
            {
                return "NHibernate.SqlClient";
            }
        }
    }

    public class Sql2005DataContext : SqlDataContext<MsSql2005Dialect>
    {
        public Sql2005DataContext() : this(null) { }

        public Sql2005DataContext(string contextKey = null)
            : base(MsSqlConfiguration.MsSql2005, contextKey)
        { }
    }

    public class Sql2008DataContext : SqlDataContext<MsSql2008Dialect>
    {
        public Sql2008DataContext() : this(null) { }

        public Sql2008DataContext(string contextKey = null)
            : base(MsSqlConfiguration.MsSql2008, contextKey)
        { }
    }

    public class Sql2012DataContext : SqlDataContext<MsSql2012Dialect>
    {
        public Sql2012DataContext() : this(null) { }

        public Sql2012DataContext(string contextKey = null)
            : base(MsSqlConfiguration.MsSql2008, contextKey)
        { }
    }
}
