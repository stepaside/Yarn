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

namespace Yarn.Data.MongoDbProvider
{
    public class Repository : IRepository, IMetaDataProvider
    {
        private PluralizationService _pluralizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
        private ConcurrentDictionary<Type, MongoCollection> _collections = new ConcurrentDictionary<Type,MongoCollection>();
        private IDataContext<MongoDatabase> _context;
        private string _prefix;

        public Repository() : this(null) { }

        public Repository(string prefix = null)
        {
            _prefix = prefix;
        }
        
        public T GetById<T, ID>(ID id) where T : class
        {
            return GetCollection<T>().FindOneByIdAs<T>(BsonValue.Create(id));
        }

        public IEnumerable<T> GetByIdList<T, ID>(IList<ID> ids) where T : class
        {
            var primaryKey = ((IMetaDataProvider)this).GetPrimaryKey<T>().First();
            var query = Query.In(primaryKey, new BsonArray(ids));
            return GetCollection<T>().FindAs<T>(query);
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = this.All<T>().Where(criteria);
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0) where T : class
        {
            var results = criteria.Apply(this.All<T>());
            if (offset >= 0 && limit > 0)
            {
                results = results.Skip(offset).Take(limit);
            }
            return results;
        }

        public T Add<T>(T entity) where T : class
        {
            var result = GetCollection<T>().Insert<T>(entity);
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public T Remove<T>(T entity) where T : class
        {
            var query = MongoDB.Driver.Builders.Query.EQ("_id", BsonValue.Create(GetId<T>(entity)));
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
            var result = GetCollection<T>().Remove(MongoDB.Driver.Builders.Query.EQ("_id", BsonValue.Create(id)));
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public T Update<T>(T entity) where T : class
        {
            var result = GetCollection<T>().Save<T>(entity);
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public void Attach<T>(T entity) where T : class
        {
            Update<T>(entity); 
        }

        public void Detach<T>(T entity) where T : class
        {
            Remove<T>(entity);
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
            return FindAll<T>(criteria).LongCount();
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll<T>(criteria).LongCount();
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            IList<T> items = new T[] { };
            var args = parameters != null ? (Dictionary<string, object>)parameters : new Dictionary<string, object>();

            if (command == "aggregate")
            {
                object collection, pipeline;
                if ((args.TryGetValue("collection", out collection) && collection is string)
                    && (args.TryGetValue("pipeline", out pipeline) && pipeline is BsonArray))
                {
                    var result = _context.Session.RunCommand(new CommandDocument 
                                                        {
                                                            { "aggregate",  (string)collection},
                                                            { "pipeline", (BsonArray)pipeline }
                                                        });
                    items = result.Response["result"].AsBsonArray.Select(v => BsonSerializer.Deserialize<T>(v.AsBsonDocument)).ToArray();
                }
            }
            else if (command == "mapReduce")
            {
                object collection, map, reduce, query, sort, limit, finalize;
                if ((args.TryGetValue("collection", out collection) && collection is string)
                    && (args.TryGetValue("map", out map) && (map is string || map is BsonJavaScript))
                    && (args.TryGetValue("reduce", out reduce) && (reduce is string || reduce is BsonJavaScript)))
                {
                    var commandDoc = new CommandDocument { { "mapReduce", (string)collection } };

                    if (map is string)
                    {
                        commandDoc.Add("map", new BsonJavaScript((string)map));
                    }
                    else
                    {
                        commandDoc.Add("map", (BsonJavaScript)map);
                    }

                    if (reduce is string)
                    {
                        commandDoc.Add("reduce", new BsonJavaScript((string)reduce));
                    }
                    else
                    {
                        commandDoc.Add("reduce", (BsonJavaScript)reduce);
                    }

                    if (args.TryGetValue("query", out query) && (query is string || query is BsonDocument))
                    {
                        if (query is string)
                        {
                            commandDoc.Add("query", BsonDocument.Parse((string)query));
                        }
                        else
                        {
                            commandDoc.Add("query", (BsonDocument)query);
                        }
                    }

                    if (args.TryGetValue("sort", out sort) && (sort is string || sort is BsonDocument))
                    {
                        if (sort is string)
                        {
                            commandDoc.Add("sort", BsonDocument.Parse((string)sort));
                        }
                        else
                        {
                            commandDoc.Add("sort", (BsonDocument)sort);
                        }
                    }

                    if (args.TryGetValue("limit", out limit) && limit is int)
                    {
                        commandDoc.Add("limit", (int)limit);
                    }

                    if (args.TryGetValue("finalize", out finalize) && (finalize is string || finalize is BsonJavaScript))
                    {
                        if (finalize is string)
                        {
                            commandDoc.Add("finalize", BsonDocument.Parse((string)finalize));
                        }
                        else
                        {
                            commandDoc.Add("finalize", (BsonDocument)finalize);
                        }
                    }

                    var result = _context.Session.RunCommand(commandDoc);
                    items = result.Response["result"].AsBsonArray.Select(v => BsonSerializer.Deserialize<T>(v.AsBsonDocument)).ToArray();
                }
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
            get
            {
                if (_context == null)
                {
                    _context = new DataContext(_prefix);
                }
                return _context;
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
                _collections.Clear();
                if (_context != null)
                {
                    //_context.Session.Server.Disconnect();
                    _context = null;
                }
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
                var database = this.Database;
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

        IEnumerable<string> IMetaDataProvider.GetPrimaryKey<T>()
        {
            return new[] { BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap.MemberName };
        }

        IDictionary<string, object> IMetaDataProvider.GetPrimaryKeyValue<T>(T entity)
        {
            return new Dictionary<string, object> { { ((IMetaDataProvider)this).GetPrimaryKey<T>().First(), GetId<T>(entity) } };
        }

        #endregion
    }
}
