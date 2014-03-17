﻿using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using Yarn;
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

        public abstract IDataContextCache DataContextCache { get; }
        
        public void SaveChanges()
        {
            this.Session.Flush();
        }

        public abstract void Dispose();
    }

    public abstract class NHibernateDataContext<TThisConfiguration, TConnectionString, TDialect> : DataContext, IMigrationProvider
        where TThisConfiguration : PersistenceConfiguration<TThisConfiguration, TConnectionString>
        where TConnectionString : ConnectionStringBuilder, new()
        where TDialect : Dialect
    {
        private static ConcurrentDictionary<string, Tuple<ISessionFactory, NHibernate.Cfg.Configuration>> _sessionFactories = new ConcurrentDictionary<string, Tuple<ISessionFactory, NHibernate.Cfg.Configuration>>();
        protected PersistenceConfiguration<TThisConfiguration, TConnectionString> _configuration = null;
        private readonly string _prefix = null;
        private readonly string _nameOrConnectionString = null;
        private readonly string _assemblyNameOrLocation;
        private readonly Assembly _configurationAssembly;

        private ISession _session = SessionCache.CurrentSession;

        protected NHibernateDataContext(PersistenceConfiguration<TThisConfiguration, TConnectionString> configuration, 
                                        string prefix = null, 
                                        string nameOrConnectionString = null,
                                        string assemblyNameOrLocation = null,
                                        Assembly configurationAssembly = null)
        {
            _configuration = configuration;
            _prefix = prefix;
            _nameOrConnectionString = nameOrConnectionString;
            _configurationAssembly = configurationAssembly;
            if (configurationAssembly == null)
            {
                _assemblyNameOrLocation = assemblyNameOrLocation;
            }
        }

        protected Tuple<ISessionFactory, NHibernate.Cfg.Configuration> CreateSessionFactory(string prefix)
        {
            var tuple = _sessionFactories.GetOrAdd(prefix, key =>
            {
                var factory = ConfigureSessionFactory(key);
                return factory;
            });
            return tuple;
        }

        protected virtual Tuple<ISessionFactory, NHibernate.Cfg.Configuration> ConfigureSessionFactory(string prefix)
        {
            var assemblyKey = prefix + ".Model";
            var nameOrConnectionString = _nameOrConnectionString ?? prefix + ".Connection";

            var configurationAssembly = _configurationAssembly;
            if (configurationAssembly == null)
            {
                var assemblyNameOrLocation = _assemblyNameOrLocation ?? ConfigurationManager.AppSettings.Get(assemblyKey);

                if (Uri.IsWellFormedUriString(assemblyNameOrLocation, UriKind.Absolute))
                {
                    configurationAssembly = Assembly.LoadFrom(assemblyNameOrLocation);
                }
                else
                {
                    configurationAssembly = Assembly.Load(assemblyNameOrLocation);
                }
            }

            var connectionString = nameOrConnectionString;
            if (ConfigurationManager.ConnectionStrings[nameOrConnectionString] != null)
            {
                connectionString = ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }

            NHibernate.Cfg.Configuration config = null;

            var sessionFactory = Fluently.Configure()
                .Database(_configuration.Dialect<TDialect>().ConnectionString(connectionString))
                //.Mappings(m => m.AutoMappings.Add(AutoMap.Assembly(assembly)))
                .Mappings(m => m.FluentMappings.AddFromAssembly(configurationAssembly))
                .ExposeConfiguration(c => config = c)
                .BuildSessionFactory();

            return Tuple.Create(sessionFactory, config);
        }

        protected virtual string DefaultPrefix
        {
            get
            {
                return "NHibernate.Default";
            }
        }

        protected ISessionFactory GetDefaultSessionFactory()
        {
            return CreateSessionFactory(DefaultPrefix).Item1;
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

                    var factory = _prefix == null ? GetDefaultSessionFactory() : CreateSessionFactory(_prefix).Item1;
                    _session = factory.OpenSession();
                    SessionCache.CurrentSession = _session;
                }
                return _session;
            }
        }

        public override IDataContextCache DataContextCache
        {
            get
            {
                return SessionCache.Instance;
            }
        }

        public Stream BuildSchema()
        {
            var output = new MemoryStream();
            var session = this.Session;
            if (session != null)
            {
                var export = new SchemaExport(CreateSessionFactory(DefaultPrefix).Item2);
                export.Execute(sql => output = new MemoryStream(Encoding.UTF8.GetBytes(sql)), false, false);
            }
            return output;
        }

        public Stream UpdateSchema()
        {
            var output = new MemoryStream();
            var session = this.Session;
            if (session != null)
            {
                var update = new SchemaUpdate(CreateSessionFactory(DefaultPrefix).Item2);
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
                    SessionCache.Instance.Cleanup();
                    _session.Dispose();
                    _session = null;
                }
            }
        }
    }
}
