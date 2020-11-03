using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;
using System.IO;
using System.Text;

namespace Yarn.Data.NHibernateProvider
{
    public abstract class DataContext : IDataContext<ISession>
    {
        public abstract ISession Session { get; }

        public string Source
        {
            get
            {
                return Session.Connection.ConnectionString;
            }
        }

        public void SaveChanges()
        {
            Session.Flush();
        }

        public abstract void Dispose();
    }

    public abstract class NHibernateDataContext<TThisConfiguration, TConnectionString, TDialect> : DataContext, IMigrationProvider
        where TThisConfiguration : PersistenceConfiguration<TThisConfiguration, TConnectionString>
        where TConnectionString : ConnectionStringBuilder, new()
        where TDialect : Dialect
    {
        private static ConcurrentDictionary<string, (ISessionFactory Factory, NHibernate.Cfg.Configuration Configuration)> _sessionFactories = new ConcurrentDictionary<string, (ISessionFactory Factory, NHibernate.Cfg.Configuration Configuration)>();
        protected PersistenceConfiguration<TThisConfiguration, TConnectionString> Configuration = null;
        protected readonly string _nameOrConnectionString;
        protected readonly string _assemblyNameOrLocation;
        protected readonly Assembly _configurationAssembly;

        private ISession _session;
        protected readonly string _connectionString;

        protected NHibernateDataContext(PersistenceConfiguration<TThisConfiguration, TConnectionString> configuration, 
                                        string nameOrConnectionString = null,
                                        string assemblyNameOrLocation = null,
                                        Assembly configurationAssembly = null)
        {
            Configuration = configuration;
            _nameOrConnectionString = nameOrConnectionString;
            _configurationAssembly = configurationAssembly;
            if (configurationAssembly == null)
            {
                _assemblyNameOrLocation = assemblyNameOrLocation;
            }

            _connectionString = GetConnectionString();
            var factory = CreateSessionFactory().Factory;
            _session = factory.OpenSession();
        }

        protected string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[_nameOrConnectionString]?.ConnectionString ?? _nameOrConnectionString;
        }

        protected (ISessionFactory Factory, NHibernate.Cfg.Configuration Configuration) CreateSessionFactory()
        {
            return _sessionFactories.GetOrAdd(_connectionString, ConfigureSessionFactory);
        }

        protected virtual (ISessionFactory Factory, NHibernate.Cfg.Configuration Configuration) ConfigureSessionFactory(string connectionString)
        {
            var configurationAssembly = GetConfigurationAssembly();

            NHibernate.Cfg.Configuration config = null;

            var sessionFactory = Fluently.Configure()
                .Database(Configuration.Dialect<TDialect>().ConnectionString(connectionString))
                //.Mappings(m => m.AutoMappings.Add(AutoMap.Assembly(assembly)))
                .Mappings(m => m.FluentMappings.AddFromAssembly(configurationAssembly))
                .ExposeConfiguration(c => config = c)
                .BuildSessionFactory();

            return (Factory: sessionFactory, Configuration: config);
        }

        protected Assembly GetConfigurationAssembly()
        {
            var configurationAssembly = _configurationAssembly;
            if (configurationAssembly == null)
            {
                configurationAssembly = Uri.IsWellFormedUriString(_assemblyNameOrLocation, UriKind.Absolute) ? Assembly.LoadFrom(_assemblyNameOrLocation) : Assembly.Load(_assemblyNameOrLocation);
            }

            return configurationAssembly;
        }

        public override ISession Session
        {
            get
            {
                if (_session == null || !_session.IsOpen)
                {
                    if (_session != null)
                    {
                        _session.Dispose();
                    }

                    var factory = CreateSessionFactory().Item1;
                    _session = factory.OpenSession();
                }
                return _session;
            }
        }
        
        public Stream BuildSchema()
        {
            var output = new MemoryStream();
            var session = Session;
            if (session != null)
            {
                var export = new SchemaExport(CreateSessionFactory().Item2);
                export.Execute(sql => output = new MemoryStream(Encoding.UTF8.GetBytes(sql)), false, false);
            }
            return output;
        }

        public Stream UpdateSchema()
        {
            var output = new MemoryStream();
            var session = Session;
            if (session != null)
            {
                var update = new SchemaUpdate(CreateSessionFactory().Item2);
                update.Execute(sql => output = new MemoryStream(Encoding.UTF8.GetBytes(sql)), false);
            }
            return output;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }
            }
        }
    }
}
