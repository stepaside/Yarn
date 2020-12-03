using Microsoft.Extensions.Configuration;
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

        private readonly DataContextOptions _options;
        
        private bool? _codeFirst;
        private string _modelKey;
        private string _source;

        protected Lazy<DbContext> Context = null;

        public DataContext(DbContext dbContext)
        {
            Context = new Lazy<DbContext>(() => dbContext, true);
        }

        public DataContext(DataContextOptions options)
        {
            _options = options;
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
            var connection = DbFactory.CreateConnection(nameOrConnectionString, (IConfiguration)null) ?? DbFactory.CreateConnection(nameOrConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(nameOrConnectionString, null));
            return connection;
        }

        protected DbContext CreateDbContext()
        {
            var nameOrConnectionString = _options.NameOrConnectionString;

            _modelKey = nameOrConnectionString;
            if (_options.DbContextType != null)
            {
                _modelKey = _options.DbContextType.FullName;
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
                
                dbContext.Configuration.LazyLoadingEnabled = _options.LazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _options.ProxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _options.AutoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _options.ValidateOnSaveEnabled;

                _codeFirst = false;
            }
            else
            {
                var dbModel = modelInfo.DbModel;
                var connection = CreateConnection(nameOrConnectionString);

                dbContext = new DbContext(connection, dbModel, true);
                dbContext.Configuration.LazyLoadingEnabled = _options.LazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _options.ProxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _options.AutoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _options.ValidateOnSaveEnabled;

                try
                {
                    dbContext.Database.Initialize(false);
                }
                catch
                {
                }

                _codeFirst = true;

                if (_options.MigrationEnabled)
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
            var dbContextType = _options.DbContextType;
            var configurationAssembly = _options.ConfigurationAssembly;

            if (dbContextType == null)
            {
                if (configurationAssembly == null && !string.IsNullOrEmpty(_options.AssemblyNameOrLocation))
                {
                    configurationAssembly = Uri.IsWellFormedUriString(_options.AssemblyNameOrLocation, UriKind.Absolute) ? Assembly.LoadFrom(_options.AssemblyNameOrLocation) : Assembly.Load(_options.AssemblyNameOrLocation);
                }

                if (configurationAssembly != null)
                {
                    dbContextType = configurationAssembly.GetTypes().FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t));
                }
            }

            if (dbContextType == null)
            {
                var connection = CreateConnection(_options.NameOrConnectionString);

                var builder = new DbModelBuilder();
                if (configurationAssembly != null)
                {
                    builder.Configurations.AddFromAssembly(configurationAssembly);
                }
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
