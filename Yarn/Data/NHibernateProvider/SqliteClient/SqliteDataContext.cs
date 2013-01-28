using System;
using System.Configuration;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Dialect;

namespace Yarn.Data.NHibernateProvider.SqliteClient
{
    public class SQLiteDataContext : NHibernateDataContext<SQLiteConfiguration, ConnectionStringBuilder, SQLiteDialect>
    {
        public SQLiteDataContext() : this(null) { }

        public SQLiteDataContext(string contextKey = null)
            : base(SQLiteConfiguration.Standard.InMemory().ShowSql(), contextKey)
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

        protected override string DefaultFactoryKey
        {
            get
            {
                return "NHibernate.SqliteClient";
            }
        }
    }
}
