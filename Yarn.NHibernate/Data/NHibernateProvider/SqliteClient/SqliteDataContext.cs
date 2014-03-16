using System;
using System.Configuration;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public abstract class SQLiteDataContext : NHibernateDataContext<SQLiteConfiguration, ConnectionStringBuilder, SQLiteDialect>
    {
        public SQLiteDataContext(SQLiteConfiguration configuration, string prefix = null, string nameOrConnectionString = null)
            : base(configuration, prefix, nameOrConnectionString)
        { }

        protected override Tuple<ISessionFactory, NHibernate.Cfg.Configuration> ConfigureSessionFactory(string factoryKey)
        {
            var assemblyKey = factoryKey + ".Model";
            var assembly = Assembly.Load(ConfigurationManager.AppSettings.Get(assemblyKey));

            NHibernate.Cfg.Configuration config = null;
            var sessionFactory = Fluently.Configure()
                .Database(_configuration)
                .Mappings(m => m.FluentMappings.AddFromAssembly(assembly))
                .ExposeConfiguration(c => config = c)
                .BuildSessionFactory();

            return Tuple.Create(sessionFactory, config);
        }

        protected override string DefaultPrefix
        {
            get
            {
                return "NHibernate.SqliteClient";
            }
        }
    }
}
