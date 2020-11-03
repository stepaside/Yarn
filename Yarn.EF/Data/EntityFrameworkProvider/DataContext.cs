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

        private static readonly ConcurrentDictionary<string, ModelInfo> DbModelBuilders = new ConcurrentDictionary<string, ModelInfo>();

        private static readonly ScriptGeneratorMigrationInitializer<DbContext> DbInitializer = new ScriptGeneratorMigrationInitializer<DbContext>();

        private readonly bool _lazyLoadingEnabled;
        private readonly bool _proxyCreationEnabled;
        private readonly bool _autoDetectChangesEnabled;
        private readonly bool _validateOnSaveEnabled;
        private readonly bool _migrationEnabled;
        private readonly string _nameOrConnectionString;
        private readonly string _assemblyNameOrLocation;
        private readonly Assembly _configurationAssembly;
        private readonly Type _dbContextType;

        private bool? _codeFirst;
        private string _contextKey;
        private string _modelKey;
        private string _source;

        protected Lazy<DbContext> Context = null;

        public DataContext(DbContext dbContext)
        {
            Context = new Lazy<DbContext>(() => dbContext, true);
        }

        public DataContext(
            bool lazyLoadingEnabled = true,
            bool proxyCreationEnabled = true,
            bool autoDetectChangesEnabled = false,
            bool validateOnSaveEnabled = true,
            bool migrationEnabled = false,
            string nameOrConnectionString = null,
            string assemblyNameOrLocation = null,
            Assembly configurationAssembly = null,
            Type dbContextType = null)
        {
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
            _dbContextType = dbContextType;
            Context = new Lazy<DbContext>(InitializeDbContext, true);
        }

        private DbContext InitializeDbContext()
        {
            var context = CreateDbContext();
            _source = context.Database.Connection.ConnectionString;
            return context;
        }

        internal static DbConnection CreateConnection(string nameOrConnectionString)
        {
            var connection = DbFactory.CreateConnection(nameOrConnectionString) ?? DbFactory.CreateConnection(nameOrConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(nameOrConnectionString));
            return connection;
        }

        protected DbContext CreateDbContext()
        {
            var nameOrConnectionString = _nameOrConnectionString;

            _contextKey = _modelKey = nameOrConnectionString;
            if (_dbContextType != null)
            {
                _modelKey = _dbContextType.FullName;
            }

            DbContext dbContext = null;

            var modelInfo = DbModelBuilders.GetOrAdd(_modelKey, k => ConfigureDbModel());

            if (modelInfo.DbContextType != null)
            {
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
                    else
                    {
                        throw new Exception("Could not find matching DbContext constructor!"); 
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
            }
            else
            {
                var dbModel = modelInfo.DbModel;
                var connection = CreateConnection(nameOrConnectionString);

                dbContext = new DbContext(connection, dbModel, true);
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
            }

            return dbContext;
        }

        protected ModelInfo ConfigureDbModel()
        {
            DbCompiledModel dbModel = null;
            ConstructorInfo dbContextCtor = null;
            var dbContextType = _dbContextType;
            var configurationAssembly = _configurationAssembly;

            if (dbContextType == null)
            {
                if (configurationAssembly == null)
                {
                    configurationAssembly = Uri.IsWellFormedUriString(_assemblyNameOrLocation, UriKind.Absolute) ? Assembly.LoadFrom(_assemblyNameOrLocation) : Assembly.Load(_assemblyNameOrLocation);
                }

                dbContextType = configurationAssembly.GetTypes().FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t));
            }

            if (dbContextType == null)
            {
                var connection = CreateConnection(_nameOrConnectionString);

                var builder = new DbModelBuilder();
                builder.Configurations.AddFromAssembly(configurationAssembly);
                dbModel = builder.Build(connection).Compile();
            }
            else
            {
                dbContextCtor = dbContextType.GetConstructor(new[] { typeof(DbConnection), typeof(bool) }) ??
                                dbContextType.GetConstructor(new[] { typeof(DbConnection) }) ??
                                 dbContextType.GetConstructor(new[] { typeof(string) });
            }

            return new ModelInfo
            {
                DbModel = dbModel,
                DbContextType = dbContextType,
                DbContextConstructor = dbContextCtor
            };
        }

        public void SaveChanges()
        {
            if (!Session.Configuration.AutoDetectChangesEnabled)
            {
                Session.ChangeTracker.DetectChanges();
            }
            Session.SaveChanges();
        }

        public virtual DbContext Session
        {
            get { return Context.Value; }
        }

        public string Source
        {
            get
            {
                if (_source == null)
                {
                    return Session.Database.Connection.ConnectionString;
                }
                return _source;
            }
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
            if (!disposing) return;

            if (Context == null) return;
            
            Context.Value.Dispose();
            Context = null;
        }
    }
}
