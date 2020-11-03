using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using Yarn.Extensions;
using Yarn.Linq.Expressions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider, IBulkOperationsProvider
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, string>> ColumnMappings = new ConcurrentDictionary<Type, Dictionary<string, string>>();

        protected IDataContext<DbContext> Context;

        private readonly RepositoryOptions _options;

        public Repository(IDataContext dataContext, RepositoryOptions options)
        {
            Context = dataContext as IDataContext<DbContext>;
            _options = options;
        }

        public Repository(IDataContext dataContext) 
            : this (dataContext, new RepositoryOptions())
        { }

        public Repository(DataContextOptions dataContextOptions, RepositoryOptions options)
        {
            Context = new DataContext(dataContextOptions);
            _options = options;
        }

        public Repository(DataContextOptions dataContextOptions) 
            : this(dataContextOptions, new RepositoryOptions())
        { }

        public T GetById<T, TKey>(TKey id) where T : class
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

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = Table<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
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
            var connection = ((IObjectContextAdapter)DbContext).ObjectContext.Connection;
            var query = parameters != null
                ? DbContext.Database.SqlQuery<T>(command, parameters.Select(p => p.Value is DbParameter ? p.Value : DbFactory.CreateParameter(connection, p.Key, p.Value)).ToArray())
                : DbContext.Database.SqlQuery<T>(command);
            return query;
        }

        public T Add<T>(T entity) where T : class
        {
            try
            {
                return Table<T>().Add(entity);
            }
            finally
            {
                if (_options.CommitOnCrud)
                {
                    DbContext?.SaveChanges();
                }
            }
        }

        public T Remove<T>(T entity) where T : class
        {
            try
            {

                return Table<T>().Remove(entity);
            }
            finally
            {
                if (_options.CommitOnCrud)
                {
                    DbContext?.SaveChanges();
                }
            }
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var result = GetById<T, TKey>(id);
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
                return null;
            }

            var dbSet = Table<T>();
            if (entry.State == EntityState.Detached)
            {
                var comparer = new EntityEqualityComparer<T>(this);
                var hash = comparer.GetHashCode(entity);
                var attachedEntity = dbSet.Local.FirstOrDefault(e => comparer.GetHashCode(e) == hash);
                if (attachedEntity != null)
                {
                    // Update only root attributes for lazy loaded entities
                    if (DbContext.Configuration.LazyLoadingEnabled || !_options.MergeOnUpdate)
                    {
                        var attachedEntry = DbContext.Entry(attachedEntity);
                        attachedEntry.CurrentValues.SetValues(entity);
                        entry = attachedEntry;
                    }
                    else
                    {
                        Merge(entity, attachedEntity, null);
                        entry = DbContext.Entry(attachedEntity);
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

            if (_options.CommitOnCrud)
            {
                DbContext?.SaveChanges();
            }

            return entry.Entity;
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
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Table<T>().LongCount(criteria);
        }

        public DbSet<T> Table<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        protected DbContext DbContext
        {
            get { return ((IDataContext<DbContext>)DataContext).Session; }
        }

        public virtual IDataContext DataContext
        {
            get
            {
                return Context;
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
                if (Context != null)
                {
                    Context.Dispose();
                    Context = null;
                }
            }
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return MetaDataProvider.Current.GetPrimaryKey<T>(DbContext);
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            return MetaDataProvider.Current.GetPrimaryKeyValue(entity, DbContext);
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
            private readonly Repository _repository;
            private IQueryable<T> _query;
            private readonly List<string[]> _paths;

            public LoadService(IRepository repository)
            {
                _repository = (Repository)repository;
                _query = repository.All<T>();
                _paths = new List<string[]>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path)
                where TProperty : class
            {
                _query = _query.Include(path);
                
                var properties = path.Body.ToString().Split('.').Where(p => !p.StartsWith("Select")).Select(p => p.TrimEnd(')')).Skip(1).ToArray();
                _paths.Add(properties);

                return this;
            }

            private void ScanForPrimaryKey(Expression expresison, IDictionary<string, bool> primaryKey)
            {
                var methodCall = expresison as MethodCallExpression;
                var binaryExpression = expresison as BinaryExpression;

                if (binaryExpression != null)
                {
                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.Equal:
                        {
                            var left = binaryExpression.Left.NodeType == ExpressionType.Convert ? ((UnaryExpression)binaryExpression.Left).Operand as MemberExpression : binaryExpression.Left as MemberExpression;
                            if (left != null && left.NodeType == ExpressionType.MemberAccess && left.Expression.NodeType == ExpressionType.Parameter && left.Expression.Type == typeof(T))
                            {
                                primaryKey[left.Member.Name] = primaryKey.ContainsKey(left.Member.Name);
                            }

                            var right = binaryExpression.Right.NodeType == ExpressionType.Convert ? ((UnaryExpression)binaryExpression.Right).Operand as MemberExpression : binaryExpression.Right as MemberExpression;
                            if (right != null && right.NodeType == ExpressionType.MemberAccess && right.Expression.NodeType == ExpressionType.Parameter && right.Expression.Type == typeof(T))
                            {
                                primaryKey[right.Member.Name] = primaryKey.ContainsKey(right.Member.Name);
                            }


                            break;
                        }
                        
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                        {
                            ScanForPrimaryKey(binaryExpression.Left, primaryKey);
                            ScanForPrimaryKey(binaryExpression.Right, primaryKey);
                            break;
                        }

                        default:
                            throw new InvalidExpressionException();
                    }
                }
                else if (methodCall != null && methodCall.Object != null && methodCall.Method.Name == "Equals" && methodCall.Arguments.Count == 1)
                {
                    var left = methodCall.Object.NodeType == ExpressionType.Convert ? ((UnaryExpression)methodCall.Object).Operand as MemberExpression : methodCall.Object as MemberExpression;
                    if (left != null && left.NodeType == ExpressionType.MemberAccess && left.Expression.NodeType == ExpressionType.Parameter && left.Expression.Type == typeof(T))
                    {
                        primaryKey[left.Member.Name] = primaryKey.ContainsKey(left.Member.Name);
                    }

                    var right = methodCall.Arguments[0].NodeType == ExpressionType.Convert ? ((UnaryExpression)methodCall.Arguments[0]).Operand as MemberExpression : methodCall.Arguments[0] as MemberExpression;
                    if (right != null && right.NodeType == ExpressionType.MemberAccess && right.Expression.NodeType == ExpressionType.Parameter && right.Expression.Type == typeof(T))
                    {
                        primaryKey[right.Member.Name] = primaryKey.ContainsKey(right.Member.Name);
                    }
                }
            }

            private bool UseLocalContext(Expression<Func<T, bool>> criteria, IEnumerable<string> primaryKey)
            {
                if (typeof(T).GetInterfaces().Contains(typeof(ITenant)))
                {
                    // Hard-coded TenantId will not be detected when the interface changes
                    Expression<Func<ITenant, long>> exp = t => t.TenantId;
                    primaryKey = primaryKey.Concat(new[] { ((MemberExpression)exp.Body).Member.Name });
                    // primaryKey = primaryKey.Concat(new[] { "TenantId" });
                }
                try
                {
                    var map = primaryKey.Distinct().ToDictionary(p => p, p => false);
                    ScanForPrimaryKey(criteria.Body, map);
                    return map.All(p => p.Value);
                }
                catch
                {
                    return false;
                }
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                var primaryKey = _repository.As<IMetaDataProvider>().GetPrimaryKey<T>();
                if (!UseLocalContext(criteria, primaryKey)) return _query.FirstOrDefault(criteria);
                var found = _repository.Table<T>().Local.FirstOrDefault(criteria.Compile());
                return found ?? _query.FirstOrDefault(criteria);
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                var primaryKey = _repository.As<IMetaDataProvider>().GetPrimaryKey<T>();
                if (UseLocalContext(criteria, primaryKey))
                {
                    var entity = _repository.Table<T>().Local.FirstOrDefault(criteria.Compile());
                    if (entity != null)
                    {
                        return new[] { entity };
                    }
                }
                var query = _query.Where(criteria);
                return _repository.Page(query, offset, limit, orderBy);
            }

            public T Find(ISpecification<T> criteria)
            {
                return Find(((Specification<T>)criteria).Predicate);
            }

            public IEnumerable<T> FindAll(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null)
            {
                return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
            }

            public IQueryable<T> All()
            {
                return _query;
            }

            public T Update(T entity)
            {
                var loadedEntity = _repository.Table<T>().Find(_repository.As<IMetaDataProvider>().GetPrimaryKeyValue(entity)) ?? Find(_repository.As<IMetaDataProvider>().BuildPrimaryKeyExpression(entity));
                if (loadedEntity != null)
                {
                    _repository.Merge(entity, loadedEntity, _paths);
                }
                return loadedEntity;
            }

            public void Dispose()
            {

            }
        }

        #endregion

        #region IBulkOperationsProvider Members

        public IEnumerable<T> GetById<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(TKey));
            var idSelector = Expression.Lambda<Func<T, TKey>>(body, parameter);

            var predicate = idSelector.BuildOrExpression(ids.ToArray());

            return Table<T>().Where(predicate);
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            using (var dbContext = new DbContext(Context.Source))
            {
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
                        Dictionary<string, string> mappings = null;

                        var regexWhere = new Regex(@"(?<=WHERE\s+)(?<criteria>.*)");
                        var regexAsClause = new Regex(@"(?<=FROM\s+.*?AS\s+)(?<as>.*?)(?=(\s+|$))");

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

                            if (mappings == null)
                            {
                                mappings = ExtractColumnMappings(typeof(T), DbContext);
                            }

                            var matchWhere = regexWhere.Match(sql);
                            var matchAsClause = regexAsClause.Match(sql);

                            var asClause = matchAsClause.Groups["as"].Value.Trim();
                            var whereClause = matchWhere.Groups["criteria"].Value;
                            whereClause = whereClause.Replace(asClause + ".", "");

                            var updateBody = (MemberInitExpression)bulkOperation.Update.Body;

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

                                if (!notFirstColumn)
                                {
                                    builder.Append("UPDATE [");
                                    builder.Append(DbContext.GetTableName<T>());
                                    builder.AppendLine("]");
                                    builder.Append("SET ");
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
                                    builder.AppendFormat("[{0}] = NULL", columnName);
                                }
                                else
                                {
                                    var type = value.GetType();
                                    var isComplex = type.IsClass && !(value is IEnumerable);
                                    if (isComplex)
                                    {
                                        var primaryKey = MetaDataProvider.Current.GetPrimaryKeyValue(value, DbContext);
                                        var j = 0;
                                        foreach (var pair in columnName.Split('|').Zip(primaryKey, Tuple.Create))
                                        {
                                            builder.AppendFormat("{0} = @{1}", pair.Item1, "p" + i + "_" + j);
                                         
                                            var parameter = command.CreateParameter();
                                            parameter.ParameterName = "p" + i + "_" + j;
                                            parameter.Value = pair.Item2;
                                            command.Parameters.Add(parameter);

                                            j++;
                                        }
                                    }
                                    else if (selfReference)
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
                                                        builder.AppendFormat(isLeft ? "[{0}] = [{0}] + @{1}" : "[{0}] = @{1} + [{0}]", columnName, "p" + i);
                                                    }
                                                    else if (providerName.Contains("MySql"))
                                                    {
                                                        builder.AppendFormat(isLeft ? "[{0}] = CONCAT([{0}], @{1})" : "[{0}] = CONCAT(@{1}, [{0}])", columnName, "p" + i);
                                                    }
                                                    else
                                                    {
                                                        builder.AppendFormat(isLeft ? "[{0}] = [{0}] || @{1}" : "[{0}] = @{1} || [{0}]", columnName, "p" + i);
                                                    }
                                                }
                                                else
                                                {
                                                    builder.AppendFormat("[{0}] = [{0}] + @{1}", columnName, "p" + i);
                                                }
                                                break;
                                            }
                                            case ExpressionType.Subtract:
                                            {
                                                builder.AppendFormat(isLeft ? "[{0}] = [{0}] - @{1}" : "[{0}] = @{1} - [{0}]", columnName, "p" + i);
                                                break;
                                            }
                                            case ExpressionType.Multiply:
                                            {
                                                builder.AppendFormat("[{0}] = [{0}] * @{1}", columnName, "p" + i);
                                                break;
                                            }
                                            case ExpressionType.Divide:
                                            {
                                                builder.AppendFormat(isLeft ? "[{0}] = [{0}] / @{1}" : "[{0}] = @{1} / [{0}]", columnName, "p" + i);
                                                break;
                                            }
                                            default:
                                            {
                                                builder.AppendFormat("[{0}] = @{1}", columnName, "p" + i);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        builder.AppendFormat("[{0}] = @{1}", columnName, "p" + i);
                                    }

                                    if (!isComplex)
                                    {
                                        var parameter = command.CreateParameter();
                                        parameter.ParameterName = "p" + i;
                                        parameter.Value = value;
                                        command.Parameters.Add(parameter);
                                    }

                                    i++;
                                }
                                notFirstColumn = true;
                            }

                            if (i == 0) continue;
                         
                            builder.AppendLine();
                            builder.Append("WHERE ");
                            builder.Append(whereClause);
                            notFirst = true;
                        }

                        command.CommandTimeout = 0;
                        command.CommandText = builder.ToString();

                        if (command.CommandText.Length > 0)
                        {
                            count = command.ExecuteNonQuery();
                        }

                        scope.Complete();
                    }
                }
            }
            finally
            {
                if (destroyConnection)
                {
                    connection.Close();
                }
            }
            return count;
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            var ids = entities.Select(e => ((IMetaDataProvider)this).GetPrimaryKeyValue(e).First());
            return Delete<T, object>(ids);
        }

        public long Delete<T, TKey>(IEnumerable<TKey> ids) where T : class
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
                        var sql = All<T>().ToString();
                        var mappings = ExtractColumnMappings(typeof(T), DbContext);

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
                    connection.Close();
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
                        var regexWhere = new Regex(@"(?<=WHERE\s+)(?<criteria>.*)");
                        var regexAsClause = new Regex(@"(?<=FROM\s+.*?AS\s+)(?<as>.*?)(?=(\s+|$))");

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

                            var matchWhere = regexWhere.Match(sql);
                            var matchAsClause = regexAsClause.Match(sql);

                            var asClause = matchAsClause.Groups["as"].Value.Trim();
                            var whereClause = matchWhere.Groups["criteria"].Value;
                            whereClause = whereClause.Replace(asClause + ".", "");

                            var notFirstType = false;
                            foreach (var type in GetTypeHierarchy(typeof(T)))
                            {
                                var tableName = DbContext.GetTableName(type);
                                if (string.IsNullOrEmpty(tableName))
                                {
                                    break;
                                }

                                if (notFirstType)
                                {
                                    builder.Append(";");
                                    builder.AppendLine();
                                }

                                builder.Append("DELETE FROM ");
                                builder.Append(tableName);
                                builder.AppendLine();
                                builder.Append("WHERE ");
                                builder.Append(whereClause);
                                notFirstType = true;
                            }

                            if (notFirstType)
                            {
                                notFirst = true;
                            }
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
                    connection.Close();
                }
            }
            return count;
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(spec => ((Specification<T>)spec).Predicate).ToArray());
        }

        protected static Dictionary<string, string> ExtractColumnMappings(Type type, DbContext context)
        {
            return ColumnMappings.GetOrAdd(type, t =>
            {
                var map = context.GetColumns(type);
                var result = map.ToDictionary(p => p.PropertyName, p => p.ColumnName);
                return result;
            });
        }

        private static IEnumerable<Type> GetTypeHierarchy (Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
                yield return current;
        }

        #endregion

        #region Merge Methods

        private void Merge(object source, object target, IReadOnlyCollection<string[]> paths)
        {
            var context = DbContext;
            var comparer = new EntityEqualityComparer(this);
            var autoDetectChangesEnabled = context.Configuration.AutoDetectChangesEnabled;
            try
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Entry(target).CurrentValues.SetValues(source);
                MergeImplementation(context, source, target, comparer, null, paths, 0);
                context.ChangeTracker.DetectChanges();
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = autoDetectChangesEnabled;

                if (_options.CommitOnCrud)
                {
                    context?.SaveChanges();
                }
            }
        }

        private static void MergeImplementation(DbContext context, object source, object target, IEqualityComparer<object> comparer, HashSet<object> ancestors, IReadOnlyCollection<string[]> paths, int level)
        {
            if (source == null || target == null) return;

            if (paths != null && paths.Count == 0) return;

            (ancestors = ancestors ?? new HashSet<object>()).Add(source);

            var properties = source.GetType().GetProperties();
            if (paths != null)
            {
                var set = new HashSet<string>(paths.Select(p => p.ElementAtOrDefault(level)).Where(p => p != null));
                properties = properties.Where(p => set.Contains(p.Name)).ToArray();
            }

            foreach (var property in properties.Where(p => p.PropertyType != typeof(string) && p.CanRead && p.CanWrite && p.PropertyType.IsClass && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)))
            {
                var value = PropertyAccessor.Get(target.GetType(), target, property.Name);
                var newValue = PropertyAccessor.Get(source.GetType(), source, property.Name);

                if (ancestors.Contains(newValue))
                {
                    continue;
                }

                if (value == null && newValue != null)
                {
                    context.Entry(target).Member(property.Name).CurrentValue = newValue;
                }
                else if (newValue == null && value != null)
                {
                    context.Entry(target).Member(property.Name).CurrentValue = null;
                }
                else if (value != null)
                {
                    try
                    {
                        context.Entry(value).CurrentValues.SetValues(newValue);
                    }
                    catch (Exception ex)
                    {
                        // Merge failed as we tried to change the parent
                        // Now try actually changing the parent
                        var hash = comparer.GetHashCode(newValue);
                        var local = context.Set(newValue.GetType()).Local.Cast<object>().FirstOrDefault(e => comparer.GetHashCode(e) == hash);
                        if (local != null && local != newValue)
                        {
                            // Found unchanged locally
                            // Assign existing parent
                            PropertyAccessor.Set(target.GetType(), target, property.Name, local);
                        }
                        else
                        {
                            PropertyAccessor.Set(target.GetType(), target, property.Name, newValue);
                            context.Entry(newValue).State = EntityState.Unchanged;
                        }
                    }
                    MergeImplementation(context, newValue, value, comparer, new HashSet<object>(ancestors), paths, level + 1);
                }
            }

            foreach (var property in properties.Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)))
            {
                var colletionProperty = context.Entry(target).Collection(property.Name);
                if (colletionProperty == null) continue;

                var collection = colletionProperty.CurrentValue as IList;

                var values = ((IEnumerable)PropertyAccessor.Get(target.GetType(), target, property.Name) ?? new object[] { }).Cast<object>().ToList();
                var newValues = ((IEnumerable)PropertyAccessor.Get(source.GetType(), source, property.Name) ?? new object[] { }).Cast<object>().ToList();

                var updates = newValues.Join(values, comparer.GetHashCode, comparer.GetHashCode, Tuple.Create).ToList();
                foreach (var item in updates)
                {
                    if (ancestors.Contains(item))
                    {
                        continue;
                    }

                    var entry = context.Entry(item.Item2);
                    switch (entry.State)
                    {
                        case EntityState.Detached:
                        {
                            var hash = comparer.GetHashCode(item.Item2);
                            var attachedTarget = context.Set(item.Item2.GetType()).Local.Cast<object>().FirstOrDefault(e => comparer.GetHashCode(e) == hash);
                            if (attachedTarget != null)
                            {
                                entry = context.Entry(attachedTarget);
                                entry.CurrentValues.SetValues(item.Item1);
                            }
                        }
                            break;
                        case EntityState.Unchanged:
                            entry.CurrentValues.SetValues(item.Item1);
                            break;
                    }

                    MergeImplementation(context, item.Item1, item.Item2, comparer, new HashSet<object>(ancestors), paths, level + 1);
                }

                var deletes = values.Except(newValues, comparer).ToList();
                foreach (var item in deletes)
                {
                    if (ancestors.Contains(item)) continue;

                    if (collection == null) continue;

                    collection.Remove(item);
                    context.Entry(item).State = EntityState.Deleted;
                }

                var inserts = newValues.Where(e => !values.Any(f => comparer.Equals(e, f))).ToList();
                foreach (var item in inserts)
                {
                    if (ancestors.Contains(item)) continue;

                    if (collection == null) continue;

                    collection.Add(item);

                    var entry = context.Entry(item);

                    var objectProperties = item.GetType().GetProperties().Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string));

                    foreach (var objectProperty in objectProperties)
                    {
                        var member = entry.Member(objectProperty.Name);
                        if (member == null || member.EntityEntry.State != EntityState.Detached || member.CurrentValue == null) continue;

                        var hash = comparer.GetHashCode(member.CurrentValue);
                        var local = context.Set(member.CurrentValue.GetType()).Local.Cast<object>().FirstOrDefault(e => comparer.GetHashCode(e) == hash);
                        if (local != null && local != member.CurrentValue)
                        {
                            // Found unchanged locally
                            // Assign existing parent
                            PropertyAccessor.Set(item.GetType(), item, objectProperty.Name, local);
                        }
                    }

                    entry.State = EntityState.Added;
                }
            }
        }

        private class EntityEqualityComparer : IEqualityComparer<object>
        {
            private readonly Repository _repository;

            public EntityEqualityComparer(Repository repository)
            {
                _repository = repository;
            }

            private object[] GetPrimaryKey(object entity)
            {
                if (entity == null)
                {
                    return new object[] { };
                }

                var method = typeof(IMetaDataProvider).GetMethod("GetPrimaryKeyValue")?.MakeGenericMethod(entity.GetType());
                return (object[])method?.Invoke(_repository.As<IMetaDataProvider>(), new[] { entity });
            }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return ArraysEqual(GetPrimaryKey(x), GetPrimaryKey(y));
            }

            public int GetHashCode(object obj)
            {
                return string.Join("::", GetPrimaryKey(obj).Select(v => v + "")).GetHashCode();
            }
        }

        private class EntityEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            private readonly Repository _repository;

            public EntityEqualityComparer(Repository repository)
            {
                _repository = repository;
            }

            private object[] GetPrimaryKey(T entity)
            {
                var primaryKey = _repository.As<IMetaDataProvider>().GetPrimaryKeyValue(entity);
                return primaryKey;
            }

            public bool Equals(T x, T y)
            {
                return ArraysEqual(GetPrimaryKey(x), GetPrimaryKey(y));
            }

            public int GetHashCode(T obj)
            {
                return string.Join("::", GetPrimaryKey(obj).Select(v => v + "")).GetHashCode();
            }
        }

        private static bool ArraysEqual(object[] x, object[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            return !x.Where((t, i) => !t.Equals(y[i])).Any();
        }

        #endregion
    }
}
