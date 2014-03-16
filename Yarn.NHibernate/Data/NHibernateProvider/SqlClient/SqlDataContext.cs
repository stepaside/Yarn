using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.SqlClient
{
    public class SqlDataContext<TDialect> : NHibernateDataContext<MsSqlConfiguration, MsSqlConnectionStringBuilder, TDialect>
        where TDialect : MsSql2000Dialect
    {
        public SqlDataContext(MsSqlConfiguration configuration) : this(configuration, null, null) { }

        public SqlDataContext(MsSqlConfiguration configuration, string nameOrConnectionString = null) : this(configuration, null, nameOrConnectionString) { }

        public SqlDataContext(MsSqlConfiguration configuration, string prefix = null, string nameOrConnectionString = null)
            : base(configuration, prefix, nameOrConnectionString)
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
        public Sql2005DataContext() : this(null, null) { }

        public Sql2005DataContext(string nameOrConnectionString) : this(null, nameOrConnectionString) { }

        public Sql2005DataContext(string prefix = null, string nameOrConnectionString = null)
            : base(MsSqlConfiguration.MsSql2005, prefix, nameOrConnectionString)
        { }
    }

    public class Sql2008DataContext : SqlDataContext<MsSql2008Dialect>
    {
        public Sql2008DataContext() : this(null, null) { }

        public Sql2008DataContext(string nameOrConnectionString) : this(null, nameOrConnectionString) { }

        public Sql2008DataContext(string prefix = null, string nameOrConnectionString = null)
            : base(MsSqlConfiguration.MsSql2008, prefix, nameOrConnectionString)
        { }
    }

    public class Sql2012DataContext : SqlDataContext<MsSql2012Dialect>
    {
        public Sql2012DataContext() : this(null, null) { }

        public Sql2012DataContext(string nameOrConnectionString) : this(null, nameOrConnectionString) { }

        public Sql2012DataContext(string prefix = null, string nameOrConnectionString = null)
            : base(MsSqlConfiguration.MsSql2008, prefix, nameOrConnectionString)
        { }
    }
}
