using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Yarn.Data.EntityFrameworkProvider.Migrations;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DataContext : IDataContext<DbContext>, IMigrationProvider
    {
        protected class ModelInfo
        {
            public DbCompiledModel DbModel { get; set; }
            public Type DbContextType { get; set; }
            public ConstructorInfo DbContextConstructor { get; set; }
        }

        private static readonly ConcurrentDictionary<string, ModelInfo> DbModelBuilders =
            new ConcurrentDictionary<string, ModelInfo>();

        private static readonly ScriptGeneratorMigrationInitializer<DbContext> DbInitializer =
            new ScriptGeneratorMigrationInitializer<DbContext>();

        protected readonly string _prefix;
        private readonly bool _lazyLoadingEnabled;
        private readonly bool _proxyCreationEnabled;
        private readonly bool _autoDetectChangesEnabled;
        private readonly bool _validateOnSaveEnabled;
        private readonly bool _migrationEnabled;
        private readonly string _nameOrConnectionString;
        private readonly string _assemblyNameOrLocation;
        private readonly Assembly _configurationAssembly;

        private bool? _codeFirst;

        protected Lazy<DbContext> _context = null;

        public DataContext() : this(prefix: null)
        {
        }

        public DataContext(string prefix = null,
            bool lazyLoadingEnabled = true,
            bool proxyCreationEnabled = true,
            bool autoDetectChangesEnabled = false,
            bool validateOnSaveEnabled = true,
            bool migrationEnabled = false,
            string nameOrConnectionString = null,
            string assemblyNameOrLocation = null,
            Assembly configurationAssembly = null)
        {
            _prefix = prefix;
            _lazyLoadingEnabled = lazyLoadingEnabled;
            _proxyCreationEnabled = proxyCreationEnabled;
            _autoDetectChangesEnabled = autoDetectChangesEnabled;
            _validateOnSaveEnabled = validateOnSaveEnabled;
            _migrationEnabled = migrationEnabled;
            _nameOrConnectionString = nameOrConnectionString;
            _configurationAssembly = configurationAssembly;
            if (_configurationAssembly != null)
            {
                _assemblyNameOrLocation = assemblyNameOrLocation;
            }
            _context = new Lazy<DbContext>(InitializeDbContext, true);
        }

        private DbContext InitializeDbContext()
        {
            var dbContext = DbContextCache.CurrentContext;
            if (dbContext == null || dbContext.Database.Connection.State == ConnectionState.Broken)
            {
                if (dbContext != null)
                {
                    dbContext.Dispose();
                }

                dbContext = _prefix == null ? GetDefaultDbContext() : CreateDbContext(_prefix);
                DbContextCache.CurrentContext = dbContext;
            }
            return dbContext;
        }

        private DbConnection CreateConnection(string nameOrConnectionString)
        {
            DbConnection connection = null;
            var providerName = DbFactory.GetProviderInvariantNameByConnectionString(nameOrConnectionString);
            if (providerName == null)
            {
                connection = DbFactory.CreateConnection(nameOrConnectionString);
            }
            else
            {
                connection = DbFactory.CreateConnection(nameOrConnectionString, providerName);
            }
            return connection;
        }

        protected DbContext CreateDbContext(string prefix)
        {
            var modelInfo = DbModelBuilders.GetOrAdd(prefix, key => ConfigureDbModel(key));
            var nameOrConnectionString = _nameOrConnectionString ?? prefix + ".Connection";

            if (modelInfo.DbContextType != null)
            {
                DbContext dbContext = null;
                if (modelInfo.DbContextConstructor != null)
                {
                    var parameters =
                        modelInfo.DbContextConstructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    var activator = Reflection.Activator.CreateDelegate(modelInfo.DbContextType, parameters);
                    if (parameters.Length == 2 && parameters[0] == typeof (DbConnection) &&
                        parameters[1] == typeof (bool))
                    {
                        var connection = CreateConnection(nameOrConnectionString);
                        dbContext = (DbContext) activator(connection, true);
                    }
                    else if (parameters.Length == 1 && parameters[0] == typeof (DbConnection))
                    {
                        var connection = CreateConnection(nameOrConnectionString);
                        dbContext = (DbContext) activator(connection);
                    }
                    else if (parameters.Length == 1 && parameters[0] == typeof (string))
                    {
                        dbContext = (DbContext) activator(nameOrConnectionString);
                    }
                }
                else
                {
                    dbContext = (DbContext) Activator.CreateInstance(modelInfo.DbContextType);
                }

                dbContext.Configuration.LazyLoadingEnabled = _lazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _proxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _validateOnSaveEnabled;

                _codeFirst = false;

                return dbContext;
            }
            else
            {
                var dbModel = modelInfo.DbModel;
                var connection = CreateConnection(nameOrConnectionString);
                
                var dbContext = new DbContext(connection, dbModel, true);
                dbContext.Configuration.LazyLoadingEnabled = _lazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _proxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _validateOnSaveEnabled;

                try
                {
                    dbContext.Database.Initialize(false);
                }
                catch
                {
                }

                _codeFirst = true;

                if (_migrationEnabled)
                {
                    Database.SetInitializer(DbInitializer);
                }

                return dbContext;
            }
        }

        protected ModelInfo ConfigureDbModel(string prefix)
        {
            DbCompiledModel dbModel = null;
            Type dbContextType = null;
            ConstructorInfo dbContextCtor = null;

            var configurationAssembly = _configurationAssembly;
            if (configurationAssembly == null)
            {
                var assemblyKey = prefix + ".Model";
                var assemblyNameOrLocation = _assemblyNameOrLocation ??
                                             ConfigurationManager.AppSettings.Get(assemblyKey);

                if (Uri.IsWellFormedUriString(assemblyNameOrLocation, UriKind.Absolute))
                {
                    configurationAssembly = Assembly.LoadFrom(assemblyNameOrLocation);
                }
                else
                {
                    configurationAssembly = Assembly.Load(assemblyNameOrLocation);
                }
            }

            dbContextType = configurationAssembly.GetTypes().FirstOrDefault(t => typeof (DbContext).IsAssignableFrom(t));
            if (dbContextType == null)
            {
                var nameOrConnectionString = _nameOrConnectionString ?? prefix + ".Connection";
                var connection = CreateConnection(nameOrConnectionString);

                var builder = new DbModelBuilder();
                builder.Configurations.AddFromAssembly(configurationAssembly);
                dbModel = builder.Build(connection).Compile();
            }
            else
            {
                dbContextCtor = dbContextType.GetConstructor(new[] {typeof (DbConnection), typeof (bool)});
                if (dbContextCtor == null)
                {
                    dbContextCtor = dbContextType.GetConstructor(new[] {typeof (DbConnection)});
                    if (dbContextCtor == null)
                    {
                        dbContextCtor = dbContextType.GetConstructor(new[] {typeof (string)});
                    }
                }
            }

            return new ModelInfo
            {
                DbModel = dbModel,
                DbContextType = dbContextType,
                DbContextConstructor = dbContextCtor
            };
        }

        protected virtual string DefaultPrefix
        {
            get { return "EF.Default"; }
        }

        protected DbContext GetDefaultDbContext()
        {
            return CreateDbContext(DefaultPrefix);
        }

        public void SaveChanges()
        {
            Session.SaveChanges();
        }

        public virtual DbContext Session
        {
            get { return _context.Value; }
        }

        public string Source
        {
            get { return Session.Database.Connection.ConnectionString; }
        }

        public IDataContextCache DataContextCache
        {
            get { return DbContextCache.Instance; }
        }

        public Stream BuildSchema()
        {
            if (Session != null && _codeFirst.HasValue && _codeFirst.Value)
            {
                DbInitializer.Enabled = true;
                Session.Database.Initialize(true);
                DbInitializer.Enabled = false;
                if (DbInitializer.CreateSql != null)
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(DbInitializer.CreateSql));
                }
            }
            return new MemoryStream();
        }

        public Stream UpdateSchema()
        {
            if (Session != null && _codeFirst.HasValue && _codeFirst.Value)
            {
                DbInitializer.Enabled = true;
                Session.Database.Initialize(true);
                DbInitializer.Enabled = false;
                if (DbInitializer.UpdateSql != null)
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(DbInitializer.UpdateSql));
                }
            }
            return new MemoryStream();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    DbContextCache.Instance.Cleanup();
                    _context = null;
                }
            }
        }
    }
}
