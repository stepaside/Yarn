using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.ModelConfiguration;
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
            public DbModelBuilder ModelBuilder { get; set; }
            public Type DbContextType { get; set; }
            public ConstructorInfo DbContextConstructor { get; set; }
        }

        private static ConcurrentDictionary<string, ModelInfo> _dbModelBuilders = new ConcurrentDictionary<string, ModelInfo>();

        private static ScriptGeneratorMigrationInitializer<DbContext> _dbInitializer = new ScriptGeneratorMigrationInitializer<DbContext>();

        protected readonly string _prefix;
        private readonly bool _lazyLoadingEnabled;
        private readonly bool _proxyCreationEnabled;
        private readonly bool _autoDetectChangesEnabled;
        private readonly bool _validateOnSaveEnabled;
        private readonly bool _migrationEnabled;
        private readonly string _nameOrConnectionString;
        private readonly string _assemblyNameOrLocation;
        private readonly Assembly _configurationAssembly;

        private bool? _codeFirst = null;
        
        protected DbContext _context = null;

        public DataContext() : this(prefix: null) { }

        public DataContext(string prefix = null, 
                            bool lazyLoadingEnabled = true, 
                            bool proxyCreationEnabled = false, 
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
            InitializeDbContext();
        }

        protected virtual void InitializeDbContext()
        {
            _context = DbContextCache.CurrentContext;
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
            var modelInfo = _dbModelBuilders.GetOrAdd(prefix, key => ConfigureDbModel(key));
            var nameOrConnectionString = _nameOrConnectionString ?? prefix + ".Connection";
            
            if (modelInfo.DbContextType != null)
            {
                DbContext dbContext = null;
                if (modelInfo.DbContextConstructor != null)
                {
                    var parameters = modelInfo.DbContextConstructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    var activator = Reflection.Activator.CreateDelegate(modelInfo.DbContextType, parameters);
                    if (parameters.Length == 2 && parameters[0] == typeof(DbConnection) && parameters[1] == typeof(bool))
                    {
                        var connection = CreateConnection(nameOrConnectionString);
                        dbContext = (DbContext)activator(connection, true);
                    }
                    else if (parameters.Length == 1 && parameters[0] == typeof(DbConnection))
                    {
                        var connection = CreateConnection(nameOrConnectionString);
                        dbContext = (DbContext)activator(connection);
                    }
                    else if (parameters.Length == 1 && parameters[0] == typeof(string))
                    {
                        dbContext = (DbContext)activator(nameOrConnectionString);
                    }
                }
                else
                {
                    dbContext = (DbContext)Activator.CreateInstance(modelInfo.DbContextType);
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
                var connection = CreateConnection(nameOrConnectionString);
                var builder = modelInfo.ModelBuilder;
                var dbModel = builder.Build(connection);
                var objectContext = dbModel.Compile().CreateObjectContext<ObjectContext>(connection);
                objectContext.ContextOptions.LazyLoadingEnabled = _lazyLoadingEnabled;
                objectContext.ContextOptions.ProxyCreationEnabled = _proxyCreationEnabled;

                if (!objectContext.DatabaseExists())
                {
                    try
                    {
                        objectContext.CreateDatabase();
                    }
                    catch { }
                }
                
                var dbContext = new DbContext(objectContext, true);
                dbContext.Configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _validateOnSaveEnabled;

                _codeFirst = true;

                if (_migrationEnabled)
                {
                    Database.SetInitializer(_dbInitializer);
                }

                return dbContext;
            }
        }
        
        protected ModelInfo ConfigureDbModel(string prefix)
        {
            DbModelBuilder builder = null;
            Type dbContextType = null;
            ConstructorInfo dbContextCtor = null;

            var configurationAssembly = _configurationAssembly;
            if (configurationAssembly == null)
            {
                var assemblyKey = prefix + ".Model";
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

            dbContextType = configurationAssembly.GetTypes().FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t));
            if (dbContextType == null)
            {
                builder = new DbModelBuilder();
                builder.Configurations.AddFromAssembly(configurationAssembly);
            }
            else
            {
                dbContextCtor = dbContextType.GetConstructor(new[] { typeof(DbConnection), typeof(bool) });
                if (dbContextCtor == null)
                {
                    dbContextCtor = dbContextType.GetConstructor(new[] { typeof(DbConnection) });
                    if (dbContextCtor == null)
                    {
                        dbContextCtor = dbContextType.GetConstructor(new[] { typeof(string) });
                    }
                }
            }

            return new ModelInfo { ModelBuilder = builder, DbContextType = dbContextType, DbContextConstructor = dbContextCtor };
        }

        protected virtual string DefaultPrefix
        {
            get
            {
                return "EF.Default";
            }
        }

        protected DbContext GetDefaultDbContext()
        {
            return CreateDbContext(DefaultPrefix);
        }

        public void SaveChanges()
        {
            this.Session.SaveChanges();
        }

        public virtual DbContext Session
        {
            get 
            {
                if (_context == null || _context.Database.Connection.State == ConnectionState.Broken)
                {
                    if (_context != null)
                    {
                        _context.Dispose();
                    }

                    _context = _prefix == null ? GetDefaultDbContext() : CreateDbContext(_prefix);
                    DbContextCache.CurrentContext = _context;
                }
                return _context;
            }
        }

        public string Source
        {
            get 
            {
                return this.Session.Database.Connection.ConnectionString;
            }
        }

        public IDataContextCache DataContextCache
        {
            get
            {
                return DbContextCache.Instance;
            }
        }
               
        public Stream BuildSchema()
        {
            if (this.Session != null && _codeFirst.HasValue && _codeFirst.Value)
            {
                _dbInitializer.Enabled = true;
                this.Session.Database.Initialize(true);
                _dbInitializer.Enabled = false;
                if (_dbInitializer.CreateSql != null)
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(_dbInitializer.CreateSql));
                }
            }
            return new MemoryStream();
        }

        public Stream UpdateSchema()
        {
            if (this.Session != null && _codeFirst.HasValue && _codeFirst.Value)
            {
                _dbInitializer.Enabled = true;
                this.Session.Database.Initialize(true);
                _dbInitializer.Enabled = false;
                if (_dbInitializer.UpdateSql != null)
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(_dbInitializer.UpdateSql));
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
                    _context.Dispose();
                    _context = null;
                }
            }
        }
    }
}
