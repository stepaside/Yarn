using System;
using System.Configuration;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public abstract class SqLiteDataContext : NHibernateDataContext<SQLiteConfiguration, ConnectionStringBuilder, SQLiteDialect>
    {
        public SqLiteDataContext(SQLiteConfiguration configuration, 
                                string nameOrConnectionString = null, 
                                string assemblyNameOrLocation = null,
                                Assembly configurationAssembly = null)
            : base(configuration, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly)
        { }

        protected override (ISessionFactory Factory, NHibernate.Cfg.Configuration Configuration) ConfigureSessionFactory(string connectionString)
        {
            var configurationAssembly = GetConfigurationAssembly();

            NHibernate.Cfg.Configuration config = null;
            var sessionFactory = Fluently.Configure()
                .Database(Configuration)
                .Mappings(m => m.FluentMappings.AddFromAssembly(configurationAssembly))
                .ExposeConfiguration(c => config = c)
                .BuildSessionFactory();

            return (Factory: sessionFactory, Configuration: config);
        }
    }
}
