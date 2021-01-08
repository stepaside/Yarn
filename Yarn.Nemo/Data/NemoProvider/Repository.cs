using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Transactions;
using Nemo;
using Nemo.Collections;
using Nemo.Collections.Extensions;
using Nemo.Configuration;
using Nemo.Extensions;
using Nemo.Linq;
using Nemo.Reflection;
using Nemo.UnitOfWork;
using Yarn.Extensions;
using Yarn.Linq.Expressions;
using Yarn.Specification;

namespace Yarn.Data.NemoProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider, IBulkOperationsProvider
    {
        private readonly RepositoryOptions _options;
        private readonly IDataContext<DbConnection> _context;
        private static ISet<Type> _configured;

        public Repository(IDataContext<DbConnection> context)
        {
            _context = context;
            _configured = new HashSet<Type>();
            if (context is DataContext dataContext && dataContext.Options != null)
            {
                _options = new RepositoryOptions { Configuration = dataContext.Options.Configuration };
            }
        }

        public Repository(RepositoryOptions options, DataContextOptions dataContextOptions)
            : this(options, dataContextOptions, null)
        { }

        public Repository(RepositoryOptions options, DataContextOptions dataContextOptions, DbTransaction transaction)
        {
            var configuration = options.Configuration ?? dataContextOptions.Configuration;

            options.Configuration = configuration;
            dataContextOptions.Configuration = configuration;

            _options = options;
            dataContextOptions.ConnectionName = dataContextOptions.ConnectionName ?? options.Configuration?.DefaultConnectionName;
            _context = new DataContext(dataContextOptions, transaction);
            _configured = new HashSet<Type>();
        }

        protected void SetConfiguration(Type type)
        {
            if (_configured.Contains(type)) return;

            if (_options == null)
            {
                _configured.Add(type);
                return;
            }
            
            if (!_options.UseStoredProcedures && _options.Configuration != null)
            {
                _options.Configuration.SetGenerateDeleteSql(true).SetGenerateInsertSql(true).SetGenerateUpdateSql(true);
                ConfigurationFactory.Set(type, _options.Configuration);
            }
            else if (!_options.UseStoredProcedures)
            {
                var config = _options.Configuration ?? ConfigurationFactory.Get(type);
                if (config == ConfigurationFactory.DefaultConfiguration)
                {
                    config = ConfigurationFactory.CloneCurrentConfiguration().SetGenerateDeleteSql(true).SetGenerateInsertSql(true).SetGenerateUpdateSql(true);
                    ConfigurationFactory.Set(type, config);
                }
                else
                {
                    config.SetGenerateDeleteSql(true).SetGenerateInsertSql(true).SetGenerateUpdateSql(true);
                }
            }
            else if (_options.Configuration != null)
            {
                ConfigurationFactory.Set(type, _options.Configuration);
            }
            _configured.Add(type);
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            SetConfiguration(typeof(T));

            if (_options.UseStoredProcedures)
            {
                var property = GetPrimaryKey<T>().First();
                return ObjectFactory.Retrieve<T>("GetById", parameters: new[] { new Param { Name = property, Value = id } }, connection: Connection).FirstOrDefault();
            }

            return ObjectFactory.Select(this.BuildPrimaryKeyExpression<T, TKey>(id), connection: Connection, selectOption: SelectOption.FirstOrDefault).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria, limit: 1).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            SetConfiguration(typeof(T));

            if (orderBy != null)
            {
                return ObjectFactory.Select(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, orderBy: orderBy.ToArray().Select(s => new Nemo.Sorting<T> { OrderBy = s.OrderBy, Reverse = s.Reverse }).ToArray());
            }
            return ObjectFactory.Select(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, skipCount: offset);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            SetConfiguration(typeof(T));
            var request = new OperationRequest { Operation = command, OperationType = OperationType.Guess, Parameters = parameters != null ? parameters.Select(p => new Param { Name = p.Key, Value = p.Value }).ToArray() : null, Connection = Connection, Transaction = ((DataContext)DataContext).Transaction };
            var response = ObjectFactory.Execute<T>(request);
            return ObjectFactory.Translate<T>(response).ToList();
        }

        public T Add<T>(T entity) where T : class
        {
            SetConfiguration(typeof(T));
            return entity.Insert() ? entity : null;
        }
        
        public T Remove<T>(T entity) where T : class
        {
            SetConfiguration(typeof(T));
            return entity.Delete() ? entity : null;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            SetConfiguration(typeof(T));
            var entity = GetById<T, TKey>(id);
            return Remove(entity);
        }

        public T Update<T>(T entity) where T : class
        {
            SetConfiguration(typeof(T));
            return entity.Update() ? entity : null;
        }

        public long Count<T>() where T : class
        {
            SetConfiguration(typeof(T));
            return ObjectFactory.Count<T>(connection: Connection);
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            SetConfiguration(typeof(T)); 
            return ObjectFactory.Count(criteria, connection: Connection);
        }

        public IQueryable<T> All<T>() where T : class
        {
            SetConfiguration(typeof(T));
            //return LinqExtensions.Defer(() => ObjectFactory.Select<T>()).AsQueryable();
            return new NemoQueryable<T>(Connection);
        }
        
        public DbConnection Connection
        {
            get { return _context.Session; }
        }

        public IDataContext DataContext
        {
            get { return _context; }
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
                _context.Dispose();
            }
        }

        public string[] GetPrimaryKey<T>() where T : class
        {
            SetConfiguration(typeof(T));
            return ObjectFactory.GetPrimaryKeyProperties(typeof(T));
        }

        public object[] GetPrimaryKeyValue<T>(T entity) where T : class
        {
            SetConfiguration(typeof(T));
            return entity.GetPrimaryKey().Values.ToArray();
        }

        public ILoadService<T> Load<T>() where T : class
        {
            SetConfiguration(typeof(T));
            return new LoadService<T>(this);
        }

        private class LoadService<T> : ILoadService<T>
            where T : class
        {
            private readonly Repository _repository;
            private readonly List<Tuple<Type[], LambdaExpression[]>> _types;

            private static readonly MethodInfo SelectMethod = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 1);
            private static readonly MethodInfo IncludeMethod2 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Include" && m.GetGenericArguments().Length == 2);
            private static readonly MethodInfo IncludeMethod3 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Include" && m.GetGenericArguments().Length == 3);
            private static readonly MethodInfo IncludeMethod4 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Include" && m.GetGenericArguments().Length == 4);
            private static readonly MethodInfo IncludeMethod5 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Include" && m.GetGenericArguments().Length == 5);

            public LoadService(Repository repository)
            {
                _repository = repository;
                _types = new List<Tuple<Type[], LambdaExpression[]>>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                var properties = path.Body.ToString().Split('.').Where(p => !p.StartsWith("Select")).Select(p => p.TrimEnd(')')).Skip(1).ToArray();

                var type = typeof(T);
                var list = new List<Type> { type };
                var lambdas = new List<LambdaExpression>();

                foreach (var property in properties.Select(t => Reflector.GetProperty(type, t)).TakeWhile(property => property != null))
                {
                    var parent = type;
                    var pk1 = Reflector.GetPropertyMap(parent).Values.Where(p => p.IsPrimaryKey).ToList();
                    
                    type = Reflector.GetElementType(property.PropertyType) ?? property.PropertyType;
                    list.Add(type);

                    _repository.SetConfiguration(type);

                    var pk2Candidates = Reflector.GetPropertyMap(type).Values.Where(p => p.Parent != null).GroupBy(p =>p.Parent).ToDictionary(g => g.Key, g => g.ToList());

                    List<ReflectedProperty> pk2;

                    if (!pk2Candidates.TryGetValue(parent, out pk2))
                    {
                        list.Clear();
                        break;
                    }

                    var a1 = Expression.Parameter(parent);
                    var a2 = Expression.Parameter(type);

                    var equals = pk1.Join(pk2, x => x.KeyPosition, y => y.RefPosition, (p1, p2) => Expression.Equal(Expression.PropertyOrField(a1, p1.PropertyName), Expression.PropertyOrField(a2, p2.PropertyName)));

                    var body = equals.Aggregate(Expression.AndAlso);
                    var join = Expression.Lambda(body, a1, a2);
                    lambdas.Add(join);
                }

                if (list.Count > 0)
                {
                    _types.Add(Tuple.Create(list.ToArray(), lambdas.ToArray()));
                }

                return this;
            }

            public T Update(T entity)
            {
                _repository.SetConfiguration(typeof(T));

                if (_repository._options.UseStoredProcedures)
                {
                    var property = _repository.As<IMetaDataProvider>().GetPrimaryKey<T>().First();
                    var value = _repository.As<IMetaDataProvider>().GetPrimaryKeyValue(entity).First();
                    var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetById", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray(), Parameters = new[] { new Param { Name = property, Value = value } }, Connection = _repository.Connection });
                    var item = ObjectFactory.Translate<T>(response).FirstOrDefault();
                    using (ObjectScope.New(item))
                    {
                        Reflection.Mapper.Map(entity, item);
                        item.Commit();
                    }
                    return item;
                }
                return null;
            }

            public T Find(Expression<Func<T, bool>> criteria)
            {
                return FindAll(criteria, 0, 1).FirstOrDefault();
            }

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> sorting = null)
            {
                _repository.SetConfiguration(typeof(T));

                var typeCount = _types.Sum(t => t.Item1.Length);
                
                if (typeCount == 0)
                {
                    return Enumerable.Empty<T>().AsQueryable();
                }

                if (typeCount == 1)
                {
                    return _repository.FindAll(criteria, offset, limit, sorting) ;
                }

                if (_repository._options.UseStoredProcedures)
                {
                    var parameters = new List<Param>();
                    var evaluatedExpression = (Expression<Func<T, bool>>)LocalCollectionExpander.Rewrite(Evaluator.PartialEval(criteria));

                    var map = Reflector.GetPropertyNameMap(typeof(T));
                    WalkTree((BinaryExpression)evaluatedExpression.Body, ExpressionType.Default, ref parameters, map);

                    if (offset >= 0 && limit > 0)
                    {
                        parameters.Add(new Param { Name = "offset", Value = offset, DbType = DbType.Int32 });
                        parameters.Add(new Param { Name = "limit", Value = limit, DbType = DbType.Int32 });
                    }

                    if (sorting != null && sorting.OrderBy != null)
                    {
                        var memberExpression = sorting.OrderBy.Body as MemberExpression;
                        if (memberExpression != null)
                        {
                            parameters.Add(new Param { Name = "orderBy", Value = map[memberExpression.Member.Name].MappedColumnName });
                        }
                    }

                    var request = new OperationRequest { Operation = "FindAll", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray(), Parameters = parameters, Connection = _repository.Connection };
                    var response = ObjectFactory.Execute<T>(request);
                    var result = ObjectFactory.Translate<T>(response);
                    if (request.Types.Count > 1)
                    {
                        result = ((IMultiResult)result).Aggregate<T>();
                    }
                    return result;
                }
                else
                {
                    var result = sorting != null
                        ? ObjectFactory.Select(criteria, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, skipCount: offset, connection: _repository.Connection, orderBy: sorting.ToArray().Select(s => new Nemo.Sorting<T> { OrderBy = s.OrderBy, Reverse = s.Reverse }).ToArray())
                        : ObjectFactory.Select(criteria, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, skipCount: offset, connection: _repository.Connection);

                    foreach (var types in _types)
                    {
                        MethodInfo method = null;
                        switch (types.Item1.Length)
                        {
                            case 2:
                                method = IncludeMethod2;
                                break;
                            case 3:
                                method = IncludeMethod3;
                                break;
                            case 4:
                                method = IncludeMethod4;
                                break;
                            case 5:
                                method = IncludeMethod5;
                                break;
                        }

                        if (method != null)
                        {
                            result = (IEnumerable<T>)method.MakeGenericMethod(types.Item1.ToArray()).Invoke(null, new object[] { result }.Concat(types.Item2).ToArray());
                        }
                    }

                    return result;
                }
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
                _repository.SetConfiguration(typeof(T));

                var typeCount = _types.Sum(t => t.Item1.Length);

                if (typeCount == 0)
                {
                    return Enumerable.Empty<T>().AsQueryable();
                }

                if (typeCount == 1)
                {
                    return _repository.All<T>();
                }

                if (_repository._options.UseStoredProcedures)
                {
                    return LinqExtensions.Defer(() =>
                    {
                        var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetAll", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray(), Connection = _repository.Connection });
                        return ObjectFactory.Translate<T>(response);
                    }).AsQueryable();
                }
                else
                {
                    var result = ObjectFactory.Select<T>(connection: _repository.Connection);

                    foreach (var types in _types)
                    {
                        MethodInfo method = null;
                        switch (types.Item1.Length)
                        {
                            case 2:
                                method = IncludeMethod2;
                                break;
                            case 3:
                                method = IncludeMethod3;
                                break;
                            case 4:
                                method = IncludeMethod4;
                                break;
                            case 5:
                                method = IncludeMethod5;
                                break;
                        }

                        if (method != null)
                        {
                            result = (IEnumerable<T>)method.MakeGenericMethod(types.Item1.ToArray()).Invoke(null, new object[] { result }.Concat(types.Item2).ToArray());
                        }
                    }

                    return result.AsQueryable();
                }
            }

            public void Dispose()
            {
                
            }
            
            private static void WalkTree(BinaryExpression body, ExpressionType linkingType, ref List<Param> parameters, IDictionary<string, ReflectedProperty> map)
            {
                if (body.NodeType != ExpressionType.AndAlso && body.NodeType != ExpressionType.OrElse)
                {
                    Expression rest;
                    string name;
                    var left = body.Left as MemberExpression;
                    if (left != null)
                    {
                        name = left.Member.Name;
                        rest = body.Right;
                    }
                    else
                    {
                        name = ((MemberExpression)body.Right).Member.Name;
                        rest = body.Left;
                    }

                    object value;
                    var constantExpression = rest as ConstantExpression;
                    if (constantExpression != null)
                    {
                        value = constantExpression.Value;
                    }
                    else
                    {
                        var lambda = Expression.Lambda(rest, null);
                        value = lambda.Compile().DynamicInvoke();
                    }

                    parameters.Add(new Param { Name = map[name].ParameterName, Value = value });
                }
                else
                {
                    WalkTree((BinaryExpression)body.Left, body.NodeType, ref parameters, map);
                    WalkTree((BinaryExpression)body.Right, body.NodeType, ref parameters, map);
                }
            }
        }

        public IEnumerable<T> GetById<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            SetConfiguration(typeof(T));

            if (_options.UseStoredProcedures)
            {
                var property = GetPrimaryKey<T>().First();
                var idList = ids.Select(k => k.ToString()).ToDelimitedString(",");
                return ObjectFactory.Retrieve<T>("GetById", parameters: new[] { new Param { Name = property + "List", Value = idList } }, connection: Connection);
            }

            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(TKey));
            var idSelector = Expression.Lambda<Func<T, TKey>>(body, parameter);

            var predicate = idSelector.BuildOrExpression(ids.ToArray());

            return ObjectFactory.Select(predicate, connection: Connection);
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            SetConfiguration(typeof(T));

            return ObjectFactory.Insert(entities);
        }

        public long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class
        {
            return Update<T>(new BulkUpdateOperation<T> { Criteria = criteria, Update = update });
        }

        public long Update<T>(params BulkUpdateOperation<T>[] bulkOperations) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            var ids = entities.Select(e => ((IMetaDataProvider)this).GetPrimaryKeyValue(e).First());
            return Delete<T, object>(ids);
        }

        public long Delete<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            throw new NotImplementedException();
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(spec => ((Specification<T>)spec).Predicate).ToArray());
        }
    }
}
