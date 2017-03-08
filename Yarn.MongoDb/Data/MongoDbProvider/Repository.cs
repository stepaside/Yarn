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
using Yarn.Specification;

namespace Yarn.Data.MongoDbProvider
{
    public class Repository : IRepository, IMetaDataProvider, IBulkOperationsProvider
    {
        private readonly PluralizationService _pluralizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
        private readonly ConcurrentDictionary<Type, object> _collections = new ConcurrentDictionary<Type, object>();
        private IDataContext<IMongoDatabase> _context;
        private readonly string _prefix;
        private readonly string _connectionString;

        public Repository() : this(null)
        {
        }

        public Repository(string prefix = null, string connectionString = null)
        {
            _prefix = prefix;
            _connectionString = connectionString;
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            var filter = this.BuildPrimaryKeyExpression<T, TKey>(id);
            return GetCollection<T>().Find(filter).Limit(1).FirstOrDefault();
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = All<T>().Where(criteria);
            return this.Page(query, offset, limit, orderBy);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = criteria.Apply(All<T>());
            return this.Page(query, offset, limit, orderBy);
        }

        public T Add<T>(T entity) where T : class
        {
            try
            {
                GetCollection<T>().InsertOne(entity, new InsertOneOptions { BypassDocumentValidation = false });
                return entity;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public T Remove<T>(T entity) where T : class
        {
            var filter = this.BuildPrimaryKeyExpression(entity);
            var result = GetCollection<T>().FindOneAndDelete(new ExpressionFilterDefinition<T>(filter));
            return result;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var filter = this.BuildPrimaryKeyExpression<T, TKey>(id);
            var result = GetCollection<T>().FindOneAndDelete(new ExpressionFilterDefinition<T>(filter));
            return result;
        }

        public T Update<T>(T entity) where T : class
        {
            var filter = this.BuildPrimaryKeyExpression(entity);
            var result = GetCollection<T>().ReplaceOne(new ExpressionFilterDefinition<T>(filter), entity, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged && result.IsModifiedCountAvailable && result.ModifiedCount == 1 ? entity : null;
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
            return GetCollection<T>().Count(new BsonDocument());
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return All<T>().LongCount(criteria);
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            IList<T> items = new T[] { };
            var args = parameters ?? new Dictionary<string, object>();

            switch (command)
            {
                case "aggregate":
                {
                    object collection, pipeline;
                    if ((args.TryGetValue("collection", out collection) && collection is string)
                        && (args.TryGetValue("pipeline", out pipeline) && pipeline is BsonArray))
                    {
                        items = _context.Session.RunCommand(new BsonDocumentCommand<List<T>>(new CommandDocument
                        {
                            { "aggregate", (string)collection },
                            { "pipeline", (BsonArray)pipeline }
                        }));
                    }
                }
                    break;
                case "mapReduce":
                {
                    object collection, map, reduce;
                    if (args.TryGetValue("collection", out collection) && collection is string
                        && args.TryGetValue("map", out map) && (map is string || map is BsonJavaScript)
                        && args.TryGetValue("reduce", out reduce) && (reduce is string || reduce is BsonJavaScript))
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
                        if (args.TryGetValue("finalize", out finalize) && (finalize is string || finalize is BsonDocument))
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

                        items = _context.Session.RunCommand(new BsonDocumentCommand<List<T>>(commandDoc));
                    }
                }
                    break;
            }
            return items;
        }

        protected IMongoDatabase Database
        {
            get { return ((IDataContext<IMongoDatabase>)DataContext).Session; }
        }

        public IDataContext DataContext
        {
            get { return _context ?? (_context = new DataContext(_prefix, _connectionString)); }
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

        protected IMongoCollection<T> GetCollection<T>() where T : class
        {
            return (IMongoCollection<T>)_collections.GetOrAdd(typeof(T), type =>
            {
                var name = type.Name.ToLower();
                if (_pluralizer.IsSingular(name))
                {
                    name = _pluralizer.Pluralize(name);
                }
                var database = Database;

                var filter = new BsonDocument("name", name);
                if (!database.ListCollections(new ListCollectionsOptions { Filter = filter}).Any())
                {
                    database.CreateCollection(name);
                }
                return database.GetCollection<T>(name);
            });
        }

        protected object GetId<T>(T entity) where T : class
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

        public IEnumerable<T> GetById<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(TKey));
            var idSelector = Expression.Lambda<Func<T, TKey>>(body, parameter);

            var filter = idSelector.BuildOrExpression(ids.ToArray());

            return GetCollection<T>().Find<T>(filter).ToList();
        }

        public long Insert<T>(IEnumerable<T> entities) where T : class
        {
            var requests = entities.Select(e => new InsertOneModel<T>(e));
            var result = GetCollection<T>().BulkWrite(requests, new BulkWriteOptions { IsOrdered = false });
            return result.IsAcknowledged ? result.InsertedCount : 0L;
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

            if (builder == null) return 0L;

            var result = GetCollection<T>().UpdateMany(new ExpressionFilterDefinition<T>(criteria), new BsonDocumentUpdateDefinition<T>(builder.ToBsonDocument()), new UpdateOptions { IsUpsert = false });
            return result.IsAcknowledged && result.IsModifiedCountAvailable ? result.ModifiedCount : 0;
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

        public long Delete<T, TKey>(IEnumerable<TKey> ids) where T : class
        {
            var criteria = ids.Select(this.BuildPrimaryKeyExpression<T, TKey>).ToArray();
            return Delete(criteria);
        }

        public long Delete<T>(params Expression<Func<T, bool>>[] criteria) where T : class
        {
            var requests = criteria.Select(c => new DeleteOneModel<T>(new ExpressionFilterDefinition<T>(c)));
            var result = GetCollection<T>().BulkWrite(requests, new BulkWriteOptions { IsOrdered = false });
            return result.IsAcknowledged ? result.DeletedCount : 0L;
        }

        public long Delete<T>(params ISpecification<T>[] criteria) where T : class
        {
            return Delete(criteria.Select(spec => ((Specification<T>)spec).Predicate).ToArray());
        }

        #endregion
    }
}
