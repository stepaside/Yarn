using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DataContext : IDataContext<DbContext>
    {
        private static ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>> _dbModelBuilders = new ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>>();

        private bool _enableLazyLoading = true;
        private bool _recreateDatabaseIfExists = false;
        private string _prefix = null;
        private DbContext _context = DbContextCache.CurrentContext;

        public DataContext() : this(true, false, null) { }

        public DataContext(string prefix = null) : this(true, false, prefix) { }

        public DataContext(bool enableLazyLoading = true, bool recreateDatabaseIfExists = false, string prefix = null)
        {
            _enableLazyLoading = enableLazyLoading;
            _recreateDatabaseIfExists = recreateDatabaseIfExists;
            _prefix = prefix;
        }

        protected DbContext CreateDbContext(string prefix)
        {
            var tuple = _dbModelBuilders.GetOrAdd(prefix, key => ConfigureDbModel(key));

            if (tuple.Item2 != null)
            {
                return (DbContext)Activator.CreateInstance(tuple.Item2);
            }
            else
            {
                var connectionKey = prefix + ".Connection";
                var builder = tuple.Item1;
                var connection = DbFactory.CreateConnection(connectionKey);
                var dbModel = builder.Build(connection);
                var objectContext = dbModel.Compile().CreateObjectContext<ObjectContext>(connection);
                objectContext.ContextOptions.LazyLoadingEnabled = _enableLazyLoading;

                if (!objectContext.DatabaseExists())
                {
                    objectContext.CreateDatabase();
                }
                else if (_recreateDatabaseIfExists)
                {
                    objectContext.DeleteDatabase();
                    objectContext.CreateDatabase();
                }

                return new DbContext(objectContext, true);
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

        public DbContext Session
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
            if ((mappingType.BaseType != null) &&
                !mappingType.BaseType.IsAbstract &&
                mappingType.BaseType.IsGenericType)
            {
                return IsMappingClass(mappingType.BaseType);
            }
            return false;
        }
    }
}
