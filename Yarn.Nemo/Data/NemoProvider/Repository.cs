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
        // ReSharper disable once InconsistentNaming
        protected readonly bool _useStoredProcedures;
        // ReSharper disable once InconsistentNaming
        private readonly IDataContext _context;

        public Repository(bool useStoredProcedures, IConfiguration configuration = null, string connectionName = null, string connectionString = null, DbTransaction transaction = null)
        {
            _useStoredProcedures = useStoredProcedures;
            _context = new DataContext(configuration != null ? configuration.DefaultConnectionName : connectionName, connectionString, transaction);
        }

        private void SetConfiguration<T>() where T : class
        {
            if (!_useStoredProcedures) return;
            var config = ConfigurationFactory.CloneCurrentConfiguration().SetGenerateDeleteSql(true).SetGenerateInsertSql(true).SetGenerateUpdateSql(true);
            ConfigurationFactory.Set<T>(config);
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            if (_useStoredProcedures)
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
            if (orderBy != null)
            {
                return ObjectFactory.Select(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, orderBy: new Nemo.Sorting<T> { OrderBy = orderBy.OrderBy, Reverse = orderBy.Reverse });
            }
            return ObjectFactory.Select(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = command, OperationType = OperationType.Guess, Parameters = parameters != null ? parameters.Select(p => new Param { Name = p.Key, Value = p.Value }).ToArray() : null, Connection = Connection, Transaction = ((DataContext)DataContext).Transaction });
            return ObjectFactory.Translate<T>(response).ToList();
        }

        public T Add<T>(T entity) where T : class
        {
            SetConfiguration<T>();
            return entity.Insert() ? entity : null;
        }
        
        public T Remove<T>(T entity) where T : class
        {
            SetConfiguration<T>();
            return entity.Delete() ? entity : null;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            SetConfiguration<T>();
            var entity = GetById<T, TKey>(id);
            return Remove(entity);
        }

        public T Update<T>(T entity) where T : class
        {
            SetConfiguration<T>();
            return entity.Update() ? entity : null;
        }

        public long Count<T>() where T : class
        {
            SetConfiguration<T>();
            return ObjectFactory.Count<T>(connection: Connection);
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            SetConfiguration<T>();
            return ObjectFactory.Count(criteria, connection: Connection);
        }

        public IQueryable<T> All<T>() where T : class
        {
            SetConfiguration<T>();
            //return LinqExtensions.Defer(() => ObjectFactory.Select<T>()).AsQueryable();
            return new NemoQueryable<T>(Connection);
        }
        
        public DbConnection Connection
        {
            get { return ((IDataContext<DbConnection>)_context).Session; }
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
            return ObjectFactory.GetPrimaryKeyProperties(typeof(T));
        }

        public object[] GetPrimaryKeyValue<T>(T entity) where T : class
        {
            return entity.GetPrimaryKey().Values.ToArray();
        }

        public ILoadService<T> Load<T>() where T : class
        {
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

                    var pk2Candidates = Reflector.GetPropertyMap(type).Values.Where(p => p.Parent != null).GroupBy(p =>p.Parent).ToDictionary(g => g.Key, g => g.ToList());

                    List<ReflectedProperty> pk2;

                    if (!pk2Candidates.TryGetValue(parent, out pk2))
                    {
                        list.Clear();
                        break;
                    }

                    var a1 = Expression.Parameter(parent);
                    var a2 = Expression.Parameter(type);

                    var equals = pk1.Join(pk2, x => x.Position, y => y.Position, (p1, p2) => Expression.Equal(Expression.PropertyOrField(a1, p1.PropertyName), Expression.PropertyOrField(a2, p2.PropertyName)));

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
                if (_repository._useStoredProcedures)
                {
                    var property = _repository.As<IMetaDataProvider>().GetPrimaryKey<T>().First();
                    var value = _repository.As<IMetaDataProvider>().GetPrimaryKeyValue<T>(entity).First();
                    var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetById", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray(), Parameters = new[] { new Param { Name = property, Value = value } } });
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
                var typeCount = _types.Sum(t => t.Item1.Length);
                
                if (typeCount == 0)
                {
                    return Enumerable.Empty<T>().AsQueryable();
                }

                if (typeCount == 1)
                {
                    return _repository.FindAll(criteria, offset, limit, sorting) ;
                }

                if (_repository._useStoredProcedures)
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

                    var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "FindAll", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray(), Parameters = parameters });
                    return ObjectFactory.Translate<T>(response);
                }
                else
                {

                    var result = sorting != null
                        ? ObjectFactory.Select(criteria, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, orderBy: new[] { new Nemo.Sorting<T> { OrderBy = sorting.OrderBy, Reverse = sorting.Reverse } })
                        : ObjectFactory.Select(criteria, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit);

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
                var typeCount = _types.Sum(t => t.Item1.Length);

                if (typeCount == 0)
                {
                    return Enumerable.Empty<T>().AsQueryable();
                }

                if (typeCount == 1)
                {
                    return _repository.All<T>();
                }

                if (_repository._useStoredProcedures)
                {
                    return LinqExtensions.Defer(() =>
                    {
                        var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetAll", OperationType = OperationType.StoredProcedure, Types = _types.Select(t => t.Item1).Flatten().Distinct().ToArray() });
                        return ObjectFactory.Translate<T>(response);
                    }).AsQueryable();
                }
                else
                {
                    var result = ObjectFactory.Select<T>();

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
            if (_useStoredProcedures)
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
