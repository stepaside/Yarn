using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
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
        private static ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>> _dbModelBuilders = new ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>>();

        private static ScriptGeneratorMigrationInitializer<DbContext> _dbInitializer = new ScriptGeneratorMigrationInitializer<DbContext>();

        protected readonly string _prefix;
        private readonly bool _lazyLoadingEnabled;
        private readonly bool _proxyCreationEnabled;
        private readonly bool _autoDetectChangesEnabled;
        private readonly bool _validateOnSaveEnabled;
        private readonly bool _migrationEnabled;

        private bool? _codeFirst = null;
        
        protected DbContext _context = null;

        public DataContext() : this(prefix: null) { }

        public DataContext(string prefix = null, 
                            bool lazyLoadingEnabled = true, 
                            bool proxyCreationEnabled = false, 
                            bool autoDetectChangesEnabled = false,
                            bool validateOnSaveEnabled = true,
                            bool migrationEnabled = false)
        {
            _prefix = prefix;
            _lazyLoadingEnabled = lazyLoadingEnabled;
            _proxyCreationEnabled = proxyCreationEnabled;
            _autoDetectChangesEnabled = autoDetectChangesEnabled;
            _validateOnSaveEnabled = validateOnSaveEnabled;
            _migrationEnabled = migrationEnabled;
            InitializeDbContext();
        }

        protected virtual void InitializeDbContext()
        {
            _context = DbContextCache.CurrentContext;
        }

        protected DbContext CreateDbContext(string prefix)
        {
            var tuple = _dbModelBuilders.GetOrAdd(prefix, key => ConfigureDbModel(key));
            
            if (tuple.Item2 != null)
            {
                var dbContext = (DbContext)Activator.CreateInstance(tuple.Item2);
                dbContext.Configuration.LazyLoadingEnabled = _lazyLoadingEnabled;
                dbContext.Configuration.ProxyCreationEnabled = _proxyCreationEnabled;
                dbContext.Configuration.AutoDetectChangesEnabled = _autoDetectChangesEnabled;
                dbContext.Configuration.ValidateOnSaveEnabled = _validateOnSaveEnabled;

                _codeFirst = false;

                return dbContext;
            }
            else
            {
                var connectionKey = prefix + ".Connection";
                var builder = tuple.Item1;
                var connection = DbFactory.CreateConnection(connectionKey);
                var dbModel = builder.Build(connection);
                var objectContext = dbModel.Compile().CreateObjectContext<ObjectContext>(connection);
                objectContext.ContextOptions.LazyLoadingEnabled = _lazyLoadingEnabled;
                objectContext.ContextOptions.ProxyCreationEnabled = _proxyCreationEnabled;

                if (!objectContext.DatabaseExists())
                {
                    objectContext.CreateDatabase();
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

        protected Tuple<DbModelBuilder, Type> ConfigureDbModel(string prefix)
        {
            var assemblyKey = prefix + ".Model";

            Assembly assembly = null;
            var assemblyLocationOrName = ConfigurationManager.AppSettings.Get(assemblyKey);
            if (Uri.IsWellFormedUriString(assemblyLocationOrName, UriKind.Absolute))
            {
                assembly = Assembly.LoadFrom(assemblyLocationOrName);
            }
            else
            {
                assembly = Assembly.Load(assemblyLocationOrName);
            }

            DbModelBuilder builder = null;
            Type dbContextType = null;
            var hasMappingClass = false;
            var isCodeFirst = true;

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(DbContext).IsAssignableFrom(type))
                {
                    isCodeFirst = false;
                    dbContextType = type;
                    break;
                }

                if (!type.IsAbstract)
                {
                    if (type.BaseType.IsGenericType && IsMappingClass(type.BaseType))
                    {
                        if (builder == null)
                        {
                            builder = new DbModelBuilder();
                        }
                        hasMappingClass = true;
                        dynamic configurationInstance = Activator.CreateInstance(type);
                        builder.Configurations.Add(configurationInstance);
                    }
                }
            }

            if (isCodeFirst && !hasMappingClass)
            {
                throw new ConfigurationErrorsException("No mapping class found in the model assembly!");
            }

            return Tuple.Create(builder, dbContextType);
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

        private bool IsMappingClass(Type mappingType)
        {
            var baseType = typeof(EntityTypeConfiguration<>);
            if (mappingType.GetGenericTypeDefinition() == baseType)
            {
                return true;
            }
            if ((mappingType.BaseType != null) && !mappingType.BaseType.IsAbstract && mappingType.BaseType.IsGenericType)
            {
                return IsMappingClass(mappingType.BaseType);
            }
            return false;
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
