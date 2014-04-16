using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;

namespace Yarn.Data.MongoDbProvider
{
    public class Repository : IRepository, IMetaDataProvider, IBulkOperationsProvider
    {
        private readonly PluralizationService _pluralizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
        private readonly ConcurrentDictionary<Type, MongoCollection> _collections = new ConcurrentDictionary<Type,MongoCollection>();
        private IDataContext<MongoDatabase> _context;
        private readonly string _prefix;

        public Repository() : this(null) { }

        public Repository(string prefix = null)
        {
            _prefix = prefix;
        }
        
        public T GetById<T, ID>(ID id) where T : class
        {
            return GetCollection<T>().FindOneByIdAs<T>(BsonValue.Create(id));
        }
        
        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = All<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = criteria.Apply(All<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public T Add<T>(T entity) where T : class
        {
            var result = GetCollection<T>().Insert(entity);
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public T Remove<T>(T entity) where T : class
        {
            var query = Query.EQ("_id", BsonValue.Create(GetId(entity)));
            entity = GetCollection<T>().FindOne(query);
            var result = GetCollection<T>().Remove(query);
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public T Remove<T, ID>(ID id) where T : class
        {
            var entity = GetById<T, ID>(id);
            var result = GetCollection<T>().Remove(Query.EQ("_id", BsonValue.Create(id)));
            return result.DocumentsAffected > 0 ? entity : null;
        }

        public T Update<T>(T entity) where T : class
        {
            var result = GetCollection<T>().Save<T>(entity);
            return result.DocumentsAffected > 0 ? entity : null;
        }

        public void Attach<T>(T entity) where T : class
        {
            Update(entity); 
        }

        public void Detach<T>(T entity) where T : class
        {
            Remove(entity);
        }

        public IQueryable<T> All<T>() where T : class
        {
            return GetCollection<T>().AsQueryable();
        }

        public long Count<T>() where T : class
        {
            return GetCollection<T>().GetStats().ObjectCount;
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).LongCount();
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            IList<T> items = new T[] { };
            var args = parameters != null ? (Dictionary<string, object>)parameters : new Dictionary<string, object>();

            switch (command)
            {
                case "aggregate":
                {
                    object collection, pipeline;
                    if ((args.TryGetValue("collection", out collection) && collection is string)
                        && (args.TryGetValue("pipeline", out pipeline) && pipeline is BsonArray))
                    {
                        var result = _context.Session.RunCommand(new CommandDocument
                        {
                            { "aggregate", (string)collection },
                            { "pipeline", (BsonArray)pipeline }
                        });
                        items =
                            result.Response["result"].AsBsonArray.Select(
                                v => BsonSerializer.Deserialize<T>(v.AsBsonDocument)).ToArray();
                    }
                }
                    break;
                case "mapReduce":
                {
                    object collection, map, reduce;
                    if ((args.TryGetValue("collection", out collection) && collection is string)
                        && (args.TryGetValue("map", out map) && (map is string || map is BsonJavaScript))
                        && (args.TryGetValue("reduce", out reduce) && (reduce is string || reduce is BsonJavaScript)))
                    {
                        var commandDoc = new CommandDocument { { "mapReduce", (string)collection } };

                        var s1 = map as string;
                        if (s1 != null)
                        {
                            commandDoc.Add("map", new BsonJavaScript(s1));
                        }
                        else
                        {
                            commandDoc.Add("map", (BsonJavaScript)map);
                        }

                        var s2 = reduce as string;
                        if (s2 != null)
                        {
                            commandDoc.Add("reduce", new BsonJavaScript(s2));
                        }
                        else
                        {
                            commandDoc.Add("reduce", (BsonJavaScript)reduce);
                        }

                        object query;
                        if (args.TryGetValue("query", out query) && (query is string || query is BsonDocument))
                        {
                            var s = query as string;
                            if (s != null)
                            {
                                commandDoc.Add("query", BsonDocument.Parse(s));
                            }
                            else
                            {
                                commandDoc.Add("query", (BsonDocument)query);
                            }
                        }

                        object sort;
                        if (args.TryGetValue("sort", out sort) && (sort is string || sort is BsonDocument))
                        {
                            var s = sort as string;
                            if (s != null)
                            {
                                commandDoc.Add("sort", BsonDocument.Parse(s));
                            }
                            else
                            {
                                commandDoc.Add("sort", (BsonDocument)sort);
                            }
                        }

                        object limit;
                        if (args.TryGetValue("limit", out limit) && limit is int)
                        {
                            commandDoc.Add("limit", (int)limit);
                        }

                        object finalize;
                        if (args.TryGetValue("finalize", out finalize) &&
                            (finalize is string || finalize is BsonJavaScript))
                        {
                            var s = finalize as string;
                            if (s != null)
                            {
                                commandDoc.Add("finalize", BsonDocument.Parse(s));
                            }
                            else
                            {
                                commandDoc.Add("finalize", (BsonDocument)finalize);
                            }
                        }

                        var result = _context.Session.RunCommand(commandDoc);
                        items =
                            result.Response["result"].AsBsonArray.Select(
                                v => BsonSerializer.Deserialize<T>(v.AsBsonDocument)).ToArray();
                    }
                }
                    break;
            }
            return items;
        }

        protected MongoDatabase Database
        {
            get
            {
                return ((IDataContext<MongoDatabase>)DataContext).Session;
            }
        }

        public IDataContext DataContext
        {
            get { return _context ?? (_context = new DataContext(_prefix)); }
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
                _collections.Clear();
                _context = null;
            }
        }

        protected MongoCollection<T> GetCollection<T>() where T : class
        {
            return (MongoCollection<T>)_collections.GetOrAdd(typeof(T), type =>
            {
                var name = type.Name.ToLower();
                if (_pluralizer.IsSingular(name))
                {
                    name = _pluralizer.Pluralize(name);
                }
                var database = Database;
                if (!database.CollectionExists(name))
                {
                    database.CreateCollection(name);
                }
                return database.GetCollection<T>(name);
            });
        }

        protected object GetId<T>(T entity) where  T : class
        {
            return BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap.Getter(entity);
        }

        #region IMetaDataProvider Members

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            return new[] { BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap.MemberName };
        }

        object[] IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            return new[] { GetId(entity) };
        }

        #endregion

        #region IBulkOperationsProvider Members

        public IEnumerable<T> GetById<T, ID>(IEnumerable<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();
            var query = Query.In(primaryKey, new BsonArray(ids));
            return GetCollection<T>().FindAs<T>(query);
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            var operation = GetCollection<T>().InitializeUnorderedBulkOperation();
            foreach (var entity in entities)
            {
                operation.Insert(entity);
            }
            var result = operation.Execute(WriteConcern.Acknowledged);
            if (result.IsAcknowledged)
            {
                return result.InsertedCount;
            }
            return 0L;
        }

        public long Update<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> update) where T : class
        {
            UpdateBuilder builder = null;
            var expression = (MemberInitExpression)update.Body;
            foreach (var binding in expression.Bindings)
            {
                var name = binding.Member.Name;
                object value;
                var memberExpression = ((MemberAssignment)binding).Expression;

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

                if (builder == null)
                {
                    builder = MongoDB.Driver.Builders.Update.Set(name, BsonValue.Create(value));
                }
                else
                {
                    builder.Set(name, BsonValue.Create(value));
                }
            }

            var linqQuery = GetCollection<T>().AsQueryable().Where(criteria);
            var query = ((MongoQueryable<T>)linqQuery).GetMongoQuery();

            var result = GetCollection<T>()
                .Update(query, builder, new MongoUpdateOptions { WriteConcern = WriteConcern.Acknowledged });
            return result.DocumentsAffected;
        }

        public long Update<T>(params BulkUpdateOperation<T>[] bulkOperations) where T : class
        {
            return bulkOperations.Sum(t => Update(t.Criteria, t.Update));
        }

        public long Delete<T>(IEnumerable<T> entities) where T : class
        {
            var ids = entities.Select(GetId);
            return Delete<T, object>(ids);
        }

        public long Delete<T, ID>(IEnumerable<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();
            var query = Query.In(primaryKey, new BsonArray(ids));
            return GetCollection<T>().Remove(query).DocumentsAffected;
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            var count = 0L;
            for (var i = 0; i < criteria.Length; i++)
            {
                var linqQuery = GetCollection<T>().AsQueryable().Where(criteria[i]);
                var query = ((MongoQueryable<T>)linqQuery).GetMongoQuery();
                count += GetCollection<T>().Remove(query).DocumentsAffected;
            }
            return count;
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(spec => ((Specification<T>)spec).Predicate).ToArray());
        }

        #endregion
    }
}
