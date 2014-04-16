using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider, IBulkOperationsProvider
    {
        private static ConcurrentDictionary<Type, Dictionary<string, string>> _columnMappings = new ConcurrentDictionary<Type, Dictionary<string, string>>();
        
        protected IDataContext<DbContext> _context;

        protected readonly string _prefix;
        protected readonly bool _lazyLoadingEnabled;
        protected readonly bool _proxyCreationEnabled;
        protected readonly bool _autoDetectChangesEnabled;
        protected readonly bool _validateOnSaveEnabled;
        protected readonly bool _migrationEnabled;
        protected readonly string _nameOrConnectionString;
        protected readonly string _assemblyNameOrLocation;
        protected readonly Assembly _configurationAssembly;
        protected readonly Type _dbContextType;

        public Repository() : this(prefix: null) { }

        public Repository(string prefix = null, 
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
            _dbContextType = dbContextType;
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            return Table<T>().Find(id);
        }
        
        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Table<T>().FirstOrDefault(criteria);
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }
        
        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = Table<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = criteria.Apply(Table<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            return PrepareSqlQuery<T>(command, parameters).ToArray();
        }

        protected DbRawSqlQuery<T> PrepareSqlQuery<T>(string command, ParamList parameters) where T : class
        {
            var connection = DbContext.Database.Connection;
            var query = parameters != null
                ? DbContext.Database.SqlQuery<T>(command, parameters.Select(p => DbFactory.CreateParameter(connection, p.Key, p.Value)).ToArray())
                : DbContext.Database.SqlQuery<T>(command);
            return query;
        }

        public T Add<T>(T entity) where T : class
        {
            return Table<T>().Add(entity);
        }

        public T Remove<T>(T entity) where T : class
        {
            return Table<T>().Remove(entity);
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var result = GetById<T, ID>(id);
            if (result != null)
            {
                Remove(result);
            }
            return result;
        }

        public T Update<T>(T entity) where T : class
        {
            var entry = DbContext.Entry(entity);
            if (entry == null)
            {
                return entity;
            }

            var dbSet = Table<T>();
            if (entry.State == EntityState.Detached)
            {
                var attachedEntity = dbSet.Local.FirstOrDefault(this.BuildPrimaryKeyExpression(entity).Compile());
                if (attachedEntity != null)
                {
                    // Update only root attributes for lazy loaded entities
                    if (DbContext.Configuration.LazyLoadingEnabled)
                    {
                        var attachedEntry = DbContext.Entry(attachedEntity);
                        attachedEntry.CurrentValues.SetValues(entity);
                        entry = attachedEntry;
                    }
                    else
                    {
                        Mapper.Map(entity, attachedEntity);
                        if (!DbContext.Configuration.AutoDetectChangesEnabled)
                        {
                            DbContext.ChangeTracker.DetectChanges();
                        }
                        entry = DbContext.Entry(attachedEntity);
                        entry.State = EntityState.Modified;
                    }
                }
                else
                {
                    dbSet.Attach(entity);
                    entry.State = EntityState.Modified;
                }
            }
            else
            {
                entry.State = EntityState.Modified;
            }
            return entry.Entity;
        }
        
        public void Attach<T>(T entity) where T : class
        {
            // TODO: revise attach to be smarter (e.g., support for object graphs)
            Table<T>().Attach(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            ((IObjectContextAdapter)DbContext).ObjectContext.Detach(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return Table<T>();
        }

        public long Count<T>() where T : class
        {
            return Table<T>().LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public DbSet<T> Table<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        protected DbContext DbContext
        {
            get
            {
                return ((IDataContext<DbContext>)this.DataContext).Session;
            }
        }

        public virtual IDataContext DataContext
        {
            get
            {
                return _context ?? (_context = new DataContext(_prefix, _lazyLoadingEnabled, _proxyCreationEnabled,
                    _autoDetectChangesEnabled, _validateOnSaveEnabled, _migrationEnabled, _nameOrConnectionString,
                    _assemblyNameOrLocation, _configurationAssembly, _dbContextType));
            }
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
                    _context.Dispose();
                    _context = null;
                }
            }
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return ((IObjectContextAdapter)this.DbContext)
                    .ObjectContext.CreateObjectSet<T>()
                    .EntitySet.ElementType.KeyMembers.Select(k => k.Name).ToArray();
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>();
            var values = new object[primaryKey.Length];
            for (var i = 0; i < primaryKey.Length; i++)
            {
                values[i] = PropertyAccessor.Get(entity, primaryKey[i]);
            }
            return values;
        }

        #endregion

        #region ILoadServiceProvider Members

        ILoadService<T> ILoadServiceProvider.Load<T>()
        {
            return new LoadService<T>(this);
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            readonly Repository _repository;
            IQueryable<T> _query;

            public LoadService(IRepository repository)
            {
                _repository = (Repository)repository;
                _query = repository.All<T>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) 
                where TProperty : class
            {
                _query = _query.Include(path);
                return this;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return _query.FirstOrDefault(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                var query = _query.Where(criteria);
                return _repository.Page(query, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return Find(((Specification<T>)criteria).Predicate);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
            }

            public IQueryable<T> All()
            {
                return _query;
            }

            public T Update(T entity)
            {
                var loadedEntity = Find(_repository.As<IMetaDataProvider>().BuildPrimaryKeyExpression(entity));
                if (loadedEntity != null)
                {
                    _repository.Update(loadedEntity);
                }
                return loadedEntity;
            }

            public void Dispose()
            {
                
            }
        }

        #endregion

        #region IBulkOperationsProvider Members

        public IEnumerable<T> GetById<T, ID>(IEnumerable<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(ID));
            var idSelector = Expression.Lambda<Func<T, ID>>(body, parameter);

            var predicate = idSelector.BuildOrExpression(ids);

            return Table<T>().Where(predicate);
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            using (var dbContext = new DbContext(DbContext.Database.Connection.ConnectionString))
            {
                dbContext.Configuration.LazyLoadingEnabled = false;
                dbContext.Configuration.AutoDetectChangesEnabled = false;

                dbContext.Set<T>().AddRange(entities);

                return dbContext.SaveChanges();
            }
        }
        
        public long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class
        {
            return Update<T>(new BulkUpdateOperation<T> { Criteria = criteria, Update = update });
        }

        public long Update<T>(params BulkUpdateOperation<T>[] bulkOperations) where T : class
        {
            var connection = DbContext.Database.Connection;
            var destroyConnection = false;
            var count = 0L;
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                        destroyConnection = true;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        var mappings = ExtractColumnMappings<T>(DbContext);

                        var regex = new Regex("FROM\\s+(?<table>.*)\\s+AS\\s+(?<as>.*)\\s+WHERE\\s+(?<criteria>.*)");
                        var builder = new StringBuilder();
                        var notFirst = false;

                        foreach (var bulkOperation in bulkOperations)
                        {
                            if (notFirst)
                            {
                                builder.Append(";");
                                builder.AppendLine();
                            }

                            Expression criteriaExpression = bulkOperation.Criteria;
                            criteriaExpression = Evaluator.PartialEval(criteriaExpression);
                            criteriaExpression = LocalCollectionExpander.Rewrite(criteriaExpression);

                            var sql = All<T>().Where((Expression<Func<T, bool>>)criteriaExpression).ToString();
                            
                            var match = regex.Match(sql);

                            var asClause = match.Groups["as"].Value.Trim();
                            var whereClause = match.Groups["criteria"].Value;
                            whereClause = whereClause.Replace(asClause + ".", "");

                            var updateBody = (MemberInitExpression)bulkOperation.Update.Body;

                            builder.Append("UPDATE ");
                            builder.Append(DbContext.GetTableName<T>());
                            builder.AppendLine();
                            builder.Append("SET ");

                            var i = 0;
                            var notFirstColumn = false;
                            foreach (var binding in updateBody.Bindings)
                            {
                                if (notFirstColumn)
                                {
                                    builder.AppendLine(", ");
                                }

                                var name = binding.Member.Name;
                                string columnName;
                                if (!mappings.TryGetValue(name, out columnName))
                                {
                                    continue;
                                }

                                object value;
                                var memberExpression = ((MemberAssignment)binding).Expression;

                                var selfReference = false;
                                var isLeft = false;
                                var binaryExpression = memberExpression as BinaryExpression;
                                if (binaryExpression != null)
                                {
                                    if (binaryExpression.Left is MemberExpression && ((MemberExpression)binaryExpression.Left).Member.Name == name)
                                    {
                                        isLeft = true;
                                        selfReference = true;
                                        memberExpression = binaryExpression.Right;
                                    }
                                    else if (binaryExpression.Right is MemberExpression && ((MemberExpression)binaryExpression.Right).Member.Name == name)
                                    {
                                        selfReference = true;
                                        memberExpression = binaryExpression.Left;
                                    }
                                }

                                var constantExpression = memberExpression as ConstantExpression;
                                if (constantExpression != null)
                                {
                                    value = constantExpression.Value;
                                }
                                else
                                {
                                    var lambda = Expression.Lambda(memberExpression, null);
                                    value = lambda.Compile().DynamicInvoke();
                                }

                                if (value == null)
                                {
                                    builder.AppendFormat("[{0}] = NULL", name);
                                }
                                else
                                {
                                    if (selfReference)
                                    {
                                        switch (binaryExpression.NodeType)
                                        {
                                            case ExpressionType.Add:
                                            {
                                                if (value is string)
                                                {
                                                    var providerName = DbFactory.GetProviderInvariantNameByConnectionString(connection.ConnectionString);
                                                    if (providerName.Contains("SqlClient"))
                                                    {
                                                        builder.AppendFormat(isLeft ? "[{0}] = [{0}] + @{1}" : "[{0}] = @{1} + [{0}]", name, "p" + i);
                                                    }
                                                    else if (providerName.Contains("MySql"))
                                                    {
                                                        builder.AppendFormat(isLeft ? "[{0}] = CONCAT([{0}], @{1})" : "[{0}] = CONCAT(@{1}, [{0}])", name, "p" + i);
                                                    }
                                                    else
                                                    {
                                                        builder.AppendFormat(isLeft ? "[{0}] = [{0}] || @{1}" : "[{0}] = @{1} || [{0}]", name, "p" + i);
                                                    }
                                                }
                                                else
                                                {
                                                    builder.AppendFormat("[{0}] = [{0}] + @{1}", name, "p" + i);
                                                }
                                                break;
                                            }
                                            case ExpressionType.Subtract:
                                            {
                                                builder.AppendFormat(isLeft ? "[{0}] = [{0}] - @{1}" : "[{0}] = @{1} - [{0}]", name, "p" + i);
                                                break;
                                            }
                                            case ExpressionType.Multiply:
                                            {
                                                builder.AppendFormat("[{0}] = [{0}] * @{1}", name, "p" + i);
                                                break;
                                            }
                                            case ExpressionType.Divide:
                                            {
                                                builder.AppendFormat(isLeft ? "[{0}] = [{0}] / @{1}" : "[{0}] = @{1} / [{0}]", name, "p" + i);
                                                break;
                                            }
                                            default:
                                            {
                                                builder.AppendFormat("[{0}] = @{1}", name, "p" + i);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        builder.AppendFormat("[{0}] = @{1}", name, "p" + i);
                                    }

                                    var parameter = command.CreateParameter();
                                    parameter.ParameterName = "p" + i;
                                    parameter.Value = value;
                                    command.Parameters.Add(parameter);

                                    i++;
                                }
                                notFirstColumn = true;
                            }

                            builder.AppendLine();
                            builder.Append("WHERE ");
                            builder.Append(whereClause);
                            notFirst = true;
                        }

                        command.CommandTimeout = 0;
                        command.CommandText = builder.ToString();
                        count = command.ExecuteNonQuery();

                        scope.Complete();
                    }
                }
            }
            finally
            {
                if (destroyConnection)
                {
                    connection.Dispose();
                }
            }
            return count;
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            var ids = entities.Select(e => ((IMetaDataProvider)this).GetPrimaryKeyValue(e).First());
            return Delete<T, object>(ids);
        }

        public long Delete<T, ID>(IEnumerable<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var connection = DbContext.Database.Connection;
            var destroyConnection = false;
            var count = 0L;
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                        destroyConnection = true;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        var mappings = ExtractColumnMappings<T>(DbContext);

                        var builder = new StringBuilder();
                        builder.Append("DELETE FROM ");
                        builder.Append(DbContext.GetTableName<T>());
                        builder.AppendLine();
                        builder.Append("WHERE ");
                        builder.Append(mappings[primaryKey]);
                        builder.AppendLine();
                        builder.Append("IN (");
                        builder.Append(string.Join(",", ids));
                        builder.Append(")");

                        command.CommandTimeout = 0;
                        command.CommandText = builder.ToString();
                        count = command.ExecuteNonQuery();

                        scope.Complete();
                    }
                }
            }
            finally
            {
                if (destroyConnection)
                {
                    connection.Dispose();
                }
            }
            return count;
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            var connection = DbContext.Database.Connection;
            var destroyConnection = false;
            var count = 0L;
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                        destroyConnection = true;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        var regex = new Regex("FROM\\s+(?<table>.*)\\s+AS\\s+(?<as>.*)\\s+WHERE\\s+(?<criteria>.*)");
                        var builder = new StringBuilder();
                        var notFirst = false;

                        foreach (var criterion in criteria)
                        {
                            if (notFirst)
                            {
                                builder.Append(";");
                                builder.AppendLine();
                            }

                            Expression expression = criterion;
                            expression = Evaluator.PartialEval(expression);
                            expression = LocalCollectionExpander.Rewrite(expression);

                            var sql = All<T>().Where((Expression<Func<T, bool>>)expression).ToString();
                            var match = regex.Match(sql);

                            var asClause = match.Groups["as"].Value.Trim();
                            var whereClause = match.Groups["criteria"].Value;
                            whereClause = whereClause.Replace(asClause + ".", "");

                            builder.Append("DELETE FROM ");
                            builder.Append(DbContext.GetTableName<T>());
                            builder.AppendLine();
                            builder.Append("WHERE ");
                            builder.Append(whereClause);
                            notFirst = true;
                        }

                        command.CommandTimeout = 0;
                        command.CommandText = builder.ToString();
                        count = command.ExecuteNonQuery();

                        scope.Complete();
                    }
                }
            }
            finally
            {
                if (destroyConnection)
                {
                    connection.Dispose();
                }
            }
            return count;
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(spec => ((Specification<T>)spec).Predicate).ToArray());
        }

        protected static Dictionary<string, string> ExtractColumnMappings<T>(DbContext context) where T : class
        {
            const BindingFlags bindings = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

            return _columnMappings.GetOrAdd(typeof(T), type =>
            {
                var objectContext = ((IObjectContextAdapter)context).ObjectContext;

                var csModel = objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                var csItem = csModel.FirstOrDefault();

                if (csItem == null)
                {
                    return new Dictionary<string, string>();
                }

                var ocModel = objectContext.MetadataWorkspace.GetItemCollection(DataSpace.OCSpace);
                var ocItem = ocModel.FirstOrDefault(o => o.GetType().Name == "ObjectTypeMapping" && ((EdmType)o.GetType().GetProperty("ClrType", bindings).GetValue(o)).FullName == type.FullName);

                if (ocItem == null)
                {
                    return new Dictionary<string, string>();
                }

                var edmType = (EdmType)ocItem.GetType().GetProperty("EdmType", bindings).GetValue(ocItem);

                var entitySetMaps = (IList)csItem.GetType().GetProperty("EntitySetMaps", bindings).GetValue(csItem);

                foreach (var entitySetMap in entitySetMaps)
                {
                    var typeMappings = entitySetMap.GetType().GetProperty("TypeMappings", bindings).GetValue(entitySetMap) as IList;
                    if (typeMappings == null || typeMappings.Count == 0)
                    {
                        continue;
                    }

                    var typeMappingsType = typeMappings[0].GetType();
                    var types = typeMappingsType.GetProperty("Types", bindings).GetValue(typeMappings[0]) as ReadOnlyCollection<EdmType>;
                    if (types == null || types.Count == 0)
                    {
                        continue;
                    }

                    if (types[0].FullName == edmType.FullName)
                    {
                        var result = new Dictionary<string, string>();
                        var mappingFragments = typeMappingsType.GetProperty("MappingFragments", bindings).GetValue(typeMappings[0]) as IList;
                        if (mappingFragments != null && mappingFragments.Count > 0)
                        {
                            var properties = mappingFragments[0].GetType().GetProperty("Properties", bindings).GetValue(mappingFragments[0]) as IList;
                            if (properties != null)
                            {
                                foreach (var propertyMap in properties)
                                {
                                    var property = (EdmProperty)propertyMap.GetType().GetProperty("EdmProperty", bindings).GetValue(propertyMap);
                                    var column = (EdmProperty)propertyMap.GetType().GetProperty("ColumnProperty", bindings).GetValue(propertyMap);
                                    result[property.Name] = column.Name;
                                }
                            }
                        }
                        return result;
                    }
                }

                return new Dictionary<string, string>();
            });
        }

        #endregion
    }
}
