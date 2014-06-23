using System.Data;
using System.Data.Common;
using System.Reflection;
using Nemo;
using Nemo.Attributes;
using Nemo.Collections.Extensions;
using Nemo.Configuration;
using Nemo.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nemo.Linq;
using Nemo.Reflection;
using Nemo.UnitOfWork;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;
using PList = Nemo.ParamList;

namespace Yarn.Nemo.Data.NemoProvider
{
    public class Repository : IRepository, IMetaDataProvider, ILoadServiceProvider
    {
        private readonly bool _useStoredProcedures;
        private readonly IDataContext _context;
        private readonly IConfiguration _configuration;

        public Repository(bool useStoredProcedures, IConfiguration configuration = null, string connectionName = null, string connectionString = null, DbTransaction transaction = null)
        {
            _useStoredProcedures = useStoredProcedures;
            _configuration = configuration;
            _context = new DataContext(configuration != null ? configuration.DefaultConnectionName : connectionName, connectionString, transaction);
        }

        public T GetById<T, ID>(ID id) where T : class
        {
            var property = GetPrimaryKey<T>().First();
            return _useStoredProcedures
                ? ObjectFactory.Retrieve<T>("GetById", parameters: new[] { new Param { Name = property, Value = id } }, connection: Connection).FirstOrDefault() 
                : ObjectFactory.Select(this.BuildPrimaryKeyExpression<T, ID>(id), connection: Connection).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find(((Specification<T>)criteria).Predicate);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria, limit: 1).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            if (orderBy != null)
            {
                return ObjectFactory.Select(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, orderBy: Tuple.Create(orderBy, SortingOrder.Ascending));
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
            return entity.Insert() ? entity : null;
        }

        public T Remove<T>(T entity) where T : class
        {
            return entity.Delete() ? entity : null;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var entity = GetById<T, ID>(id);
            return Remove(entity);
        }

        public T Update<T>(T entity) where T : class
        {
            return entity.Update() ? entity : null;
        }

        public long Count<T>() where T : class
        {
            return ObjectFactory.Count<T>();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return ObjectFactory.Count(criteria, connection: Connection);
        }

        public IQueryable<T> All<T>() where T : class
        {
            //return LinqExtensions.Defer(() => ObjectFactory.Select<T>()).AsQueryable();
            return new NemoQueryable<T>(Connection);
        }

        public void Detach<T>(T entity) where T : class
        {
            entity.Detach();
        }

        public void Attach<T>(T entity) where T : class
        {
            entity.Attach();
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
            _context.Dispose();
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
            private readonly List<IList<Type>> _types;

            private readonly static MethodInfo _unionMethod = typeof(ObjectFactory).GetMethod("Union");
            private readonly static MethodInfo _selectMethod1 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 1);
            private readonly static MethodInfo _selectMethod2 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 2);
            private readonly static MethodInfo _selectMethod3 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 3);
            private readonly static MethodInfo _selectMethod4 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 4);
            private readonly static MethodInfo _selectMethod5 = typeof(ObjectFactory).GetMethods().First(m => m.Name == "Select" && m.GetGenericArguments().Length == 5);

            public LoadService(Repository repository)
            {
                _repository = repository;
                _types = new List<IList<Type>>();
            }

            public ILoadService<T> Include<TProperty>(Expression<Func<T, TProperty>> path) where TProperty : class
            {
                var properties = path.Body.ToString().Split('.').Where(p => !p.StartsWith("Select")).Select(p => p.TrimEnd(')')).Skip(1).ToArray();

                var type = typeof(T);
                var list = new List<Type> { type };

                foreach (var property in properties.Select(t => Reflector.GetProperty(type, t)).TakeWhile(property => property != null))
                {
                    type = Reflector.GetElementType(property.PropertyType) ?? property.PropertyType;
                    list.Add(type);
                }

                _types.Add(list);

                return this;
            }

            public T Update(T entity)
            {
                if (_repository._useStoredProcedures)
                {
                    var property = _repository.As<IMetaDataProvider>().GetPrimaryKey<T>().First();
                    var value = _repository.As<IMetaDataProvider>().GetPrimaryKeyValue<T>(entity).First();
                    var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetById", OperationType = OperationType.StoredProcedure, Types = _types.Flatten().Distinct().ToArray(), Parameters = new[] { new Param { Name = property, Value = value } } });
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

            public IEnumerable<T> FindAll(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null)
            {
                var typeCount = _types.Sum(t => t.Count);
                
                if (typeCount == 0)
                {
                    return Enumerable.Empty<T>().AsQueryable();
                }

                if (typeCount == 1)
                {
                    return _repository.FindAll(criteria, offset, limit, orderBy) ;
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

                    if (orderBy != null)
                    {
                        var memberExpression = orderBy.Body as MemberExpression;
                        if (memberExpression != null)
                        {
                            parameters.Add(new Param { Name = "orderBy", Value = map[memberExpression.Member.Name].MappedColumnName });
                        }
                    }

                    var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "FindAll", OperationType = OperationType.StoredProcedure, Types = _types.Flatten().Distinct().ToArray(), Parameters = parameters });
                    return ObjectFactory.Translate<T>(response);
                }
                else
                {
                    var queries = new List<IEnumerable<T>>();
                    var args = new object[] { criteria, null, null, limit > 0 ? offset/limit + 1 : 0, limit, null, SelectOption.All, new[] { Tuple.Create(orderBy, SortingOrder.Ascending) } };
                    foreach (var types in _types)
                    {
                        MethodInfo method = null;
                        switch (types.Count)
                        {
                            case 1:
                                method = _selectMethod1;
                                break;
                            case 2:
                                method = _selectMethod2;
                                break;
                            case 3:
                                method = _selectMethod3;
                                break;
                            case 4:
                                method = _selectMethod4;
                                break;
                            case 5:
                                method = _selectMethod5;
                                break;
                        }

                        if (method != null)
                        {
                            queries.Add((IEnumerable<T>)method.MakeGenericMethod(types.ToArray()).Invoke(null, args));
                        }
                    }

                    return (IEnumerable<T>)_unionMethod.Invoke(null, new object[] { queries.ToArray() });
                }
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
                var typeCount = _types.Sum(t => t.Count);

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
                        var response = ObjectFactory.Execute<T>(new OperationRequest { Operation = "GetAll", OperationType = OperationType.StoredProcedure, Types = _types.Flatten().Distinct().ToArray() });
                        return ObjectFactory.Translate<T>(response);
                    }).AsQueryable();
                }
                else
                {
                    var queries = new List<IEnumerable<T>>();
                    var args = new object[] { null, null, null, 0, 0, null, SelectOption.All, new Tuple<Expression<Func<T, object>>, SortingOrder>[] { } };
                    foreach (var types in _types)
                    {
                        MethodInfo method = null;
                        switch (types.Count)
                        {
                            case 1:
                                method = _selectMethod1;
                                break;
                            case 2:
                                method = _selectMethod2;
                                break;
                            case 3:
                                method = _selectMethod3;
                                break;
                            case 4:
                                method = _selectMethod4;
                                break;
                            case 5:
                                method = _selectMethod5;
                                break;
                        }

                        if (method != null)
                        {
                            queries.Add((IEnumerable<T>)method.MakeGenericMethod(types.ToArray()).Invoke(null, args));
                        }
                    }

                    return LinqExtensions.Defer(() => (IEnumerable<T>)_unionMethod.Invoke(null, new object[] { queries.ToArray() })).AsQueryable();
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
    }
}
