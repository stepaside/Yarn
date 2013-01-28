using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using Yarn;
using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using System.Data.Entity.ModelConfiguration;
using System.Data.Objects;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DataContext : IDataContext<DbContext>
    {
        private static ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>> _dbModelBuilders = new ConcurrentDictionary<string, Tuple<DbModelBuilder, Type>>();

        private bool _enableLazyLoading = true;
        private bool _recreateDatabaseIfExists = false;
        private string _contextKey = null;
        private DbContext _context = DbContextCache.CurrentContext;

        public DataContext() : this(true, false, null) { }

        public DataContext(bool enableLazyLoading = true, bool recreateDatabaseIfExists = false, string contextKey = null)
        {
            _enableLazyLoading = enableLazyLoading;
            _recreateDatabaseIfExists = recreateDatabaseIfExists;
            _contextKey = contextKey;
        }

        protected DbContext CreateDbContext(string contextKey)
        {
            var tuple = _dbModelBuilders.GetOrAdd(contextKey, key => ConfigureDbModel(key));
            var connectionKey = contextKey + ".Connection";

            if (tuple.Item2 != null)
            {
                return (DbContext)Activator.CreateInstance(tuple.Item2, new object[] { connectionKey });
            }
            else
            {
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

        protected Tuple<DbModelBuilder, Type> ConfigureDbModel(string contextKey)
        {
            var assemblyKey = contextKey + ".Model";
            var connectionKey = contextKey + ".Connection";

            Assembly assembly = null;
            var assemblyLocation = ConfigurationManager.AppSettings.Get(assemblyKey);
            if (Uri.IsWellFormedUriString(assemblyLocation, UriKind.Absolute))
            {
                assembly = Assembly.LoadFrom(assemblyLocation);
            }
            else
            {
                assembly = Assembly.Load(assemblyLocation);
            }

            DbModelBuilder builder = null;
            Type dbContextType = null;
            var hasMappingClass = false;
            var isCodeFirst = true;

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(DbContext).IsAssignableFrom(type) && type.GetConstructors().Select(c => c.GetParameters()).Any(p => p.Length == 1 && p[0].ParameterType == typeof(string)))
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

        protected virtual string DefaultContextKey
        {
            get
            {
                return "EF.Default";
            }
        }

        protected DbContext GetDefaultDbContext()
        {
            return CreateDbContext(DefaultContextKey);
        }

        public void SaveChanges()
        {
            this.Session.SaveChanges();
        }

        public DbContext Session
        {
            get 
            {
                if (_context == null)
                {
                    _context = _contextKey == null ? GetDefaultDbContext() : CreateDbContext(_contextKey);
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
