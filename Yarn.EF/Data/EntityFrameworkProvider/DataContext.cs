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
            public bool AcceptsNameOrConnectionString { get; set; }
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

        private bool? _codeFirst = null;
        
        protected DbContext _context = null;

        public DataContext() : this(prefix: null) { }

        public DataContext(string prefix = null, 
                            bool lazyLoadingEnabled = true, 
                            bool proxyCreationEnabled = false, 
                            bool autoDetectChangesEnabled = false,
                            bool validateOnSaveEnabled = true,
                            bool migrationEnabled = false,
                            string nameOrConnectionString = null)
        {
            _prefix = prefix;
            _lazyLoadingEnabled = lazyLoadingEnabled;
            _proxyCreationEnabled = proxyCreationEnabled;
            _autoDetectChangesEnabled = autoDetectChangesEnabled;
            _validateOnSaveEnabled = validateOnSaveEnabled;
            _migrationEnabled = migrationEnabled;
            _nameOrConnectionString = nameOrConnectionString;
            InitializeDbContext();
        }

        protected virtual void InitializeDbContext()
        {
            _context = DbContextCache.CurrentContext;
        }

        protected DbContext CreateDbContext(string prefix)
        {
            var modelInfo = _dbModelBuilders.GetOrAdd(prefix, key => ConfigureDbModel(key));
            var nameOrConnectionString = _nameOrConnectionString ?? prefix + ".Connection";

            if (modelInfo.DbContextType != null)
            {
                var dbContext = modelInfo.AcceptsNameOrConnectionString ? (DbContext)Activator.CreateInstance(modelInfo.DbContextType, nameOrConnectionString) : (DbContext)Activator.CreateInstance(modelInfo.DbContextType);
                dbContext.Configuration.LazyLoadingEnabled = _lazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _proxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _validateOnSaveEnabled;

                _codeFirst = false;

                return dbContext;
            }
            else
            {
                DbConnection connection = null;
                var builder = modelInfo.ModelBuilder;
                var providerName = DbFactory.GetProviderInvariantNameByConnectionString(nameOrConnectionString);
                if (providerName == null)
                {
                    connection = DbFactory.CreateConnection(nameOrConnectionString);
                }
                else
                {
                    connection = DbFactory.CreateConnection(nameOrConnectionString, providerName);
                }
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
            var assemblyKey = prefix + ".Model";

            DbModelBuilder builder = null;
            Type dbContextType = null;
            var acceptsNameOrConnectionString = false;
            
            var assemblies = new List<Assembly>();
            var assemblyLocationsOrNames = ConfigurationManager.AppSettings.Get(assemblyKey);
            foreach (var locationOrName in assemblyLocationsOrNames.Split('|'))
            {
                Assembly assembly = null;
                if (Uri.IsWellFormedUriString(locationOrName, UriKind.Absolute))
                {
                    assembly = Assembly.LoadFrom(locationOrName);
                }
                else
                {
                    assembly = Assembly.Load(locationOrName);
                }
                assemblies.Add(assembly);
            }

            dbContextType = assemblies.SelectMany(a => a.GetTypes()).FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t));
            if (dbContextType == null)
            {
                builder = new DbModelBuilder();
                foreach (var assembly in assemblies)
                {
                    builder.Configurations.AddFromAssembly(assembly);
                }
            }
            else
            {
               acceptsNameOrConnectionString = dbContextType.GetConstructor(new[] { typeof(string) }) != null;
            }

            return new ModelInfo { ModelBuilder = builder, DbContextType = dbContextType, AcceptsNameOrConnectionString = acceptsNameOrConnectionString };
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
    }
}
