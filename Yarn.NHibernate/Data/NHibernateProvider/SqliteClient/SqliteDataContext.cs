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

        protected override Tuple<ISessionFactory, NHibernate.Cfg.Configuration> ConfigureSessionFactory()
        {
            var configurationAssembly = _configurationAssembly;
            if (configurationAssembly == null)
            {
                configurationAssembly = Uri.IsWellFormedUriString(_assemblyNameOrLocation, UriKind.Absolute) ? Assembly.LoadFrom(_assemblyNameOrLocation) : Assembly.Load(_assemblyNameOrLocation);
            }

            NHibernate.Cfg.Configuration config = null;
            var sessionFactory = Fluently.Configure()
                .Database(Configuration)
                .Mappings(m => m.FluentMappings.AddFromAssembly(configurationAssembly))
                .ExposeConfiguration(c => config = c)
                .BuildSessionFactory();

            return Tuple.Create(sessionFactory, config);
        }
    }
}
