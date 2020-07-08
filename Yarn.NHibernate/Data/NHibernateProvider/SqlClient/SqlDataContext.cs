using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using System.Reflection;

namespace Yarn.Data.NHibernateProvider.SqlClient
{
    public class SqlDataContext<TDialect> : NHibernateDataContext<MsSqlConfiguration, MsSqlConnectionStringBuilder, TDialect>
        where TDialect : MsSql2000Dialect
    {
        public SqlDataContext(MsSqlConfiguration configuration) : this(configuration, null, null, null) { }

        public SqlDataContext(MsSqlConfiguration configuration, string assemblyNameOrLocation = null) : this(configuration, null, assemblyNameOrLocation, null) { }

        public SqlDataContext(MsSqlConfiguration configuration, Assembly configurationAssembly = null) : this(configuration, null, null, configurationAssembly) { }

        public SqlDataContext(MsSqlConfiguration configuration, string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(configuration, nameOrConnectionString, assemblyNameOrLocation, null) { }

        public SqlDataContext(MsSqlConfiguration configuration, string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(configuration, nameOrConnectionString, null, configurationAssembly) { }

        public SqlDataContext(MsSqlConfiguration configuration, string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(configuration, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }

    public class Sql2005DataContext : SqlDataContext<MsSql2005Dialect>
    {
        public Sql2005DataContext() : this(null, null, null) { }

        public Sql2005DataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public Sql2005DataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public Sql2005DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public Sql2005DataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public Sql2005DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(MsSqlConfiguration.MsSql2005, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }

    public class Sql2008DataContext : SqlDataContext<MsSql2008Dialect>
    {
        public Sql2008DataContext() : this(null, null, null) { }

        public Sql2008DataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public Sql2008DataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public Sql2008DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public Sql2008DataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public Sql2008DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(MsSqlConfiguration.MsSql2008, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }

    public class Sql2012DataContext : SqlDataContext<MsSql2012Dialect>
    {
        public Sql2012DataContext() : this(null, null, null) { }

        public Sql2012DataContext(string assemblyNameOrLocation = null) : this(null, assemblyNameOrLocation, null) { }

        public Sql2012DataContext(Assembly configurationAssembly = null) : this(null, null, configurationAssembly) { }

        public Sql2012DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null) : this(nameOrConnectionString, assemblyNameOrLocation, null) { }

        public Sql2012DataContext(string nameOrConnectionString = null, Assembly configurationAssembly = null) : this(nameOrConnectionString, null, configurationAssembly) { }

        public Sql2012DataContext(string nameOrConnectionString = null, string assemblyNameOrLocation = null, Assembly configurationAssembly = null)
            : base(MsSqlConfiguration.MsSql2012, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }
    }
}
