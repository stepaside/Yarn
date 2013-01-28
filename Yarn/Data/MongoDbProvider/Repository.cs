using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Yarn;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Raven.Client;
using Raven.Client.Linq;
using Yarn.Reflection;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;

namespace Yarn.Data.MongoDbProvider
{
    public class Repository : IRepository
    {
        private PluralizationService _pluralizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
        private ConcurrentDictionary<Type, MongoCollection> _collections = new ConcurrentDictionary<Type,MongoCollection>();
        private IDataContext<MongoDatabase> _context;
        private string _contextKey;

        public Repository() : this(null) { }

        public Repository(string contextKey = null)
        {
            _contextKey = contextKey;
        }
        
        public T GetById<T, ID>(ID id) where T : class
        {
            return GetCollection<T>().FindOneByIdAs<T>(BsonValue.Create(id));
        }
       
        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.FindAll(criteria).FirstOrDefault();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return FindAll(criteria).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return this.All<T>().Where(criteria);
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria) where T : class
        {
            return criteria.Apply(this.All<T>());
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

        public T Merge<T>(T entity) where T : class
        {
            var result = GetCollection<T>().Save<T>(entity);
            if (result.DocumentsAffected > 0)
            {
                return entity;
            }
            return null;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public void Attach<T>(T entity) where T : class
        {
            Merge<T>(entity); 
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

        public IList<T> Execute<T>(string command, params System.Tuple<string, object>[] parameters) where T : class
        {
            throw new NotSupportedException();
        }

        public IDataContext<MongoDatabase> PrivateContext
        {
            get
            {
                return (IDataContext<MongoDatabase>)DataContext;
            }
        }

        public IDataContext DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new DataContext(_contextKey);
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
                if (!PrivateContext.Session.CollectionExists(name))
                {
                    PrivateContext.Session.CreateCollection(name);
                }
                return PrivateContext.Session.GetCollection<T>(name);
            });
        }

        protected object GetId<T>(T entity) where  T : class
        {
            return BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap.Getter(entity);
        }
    }
}
