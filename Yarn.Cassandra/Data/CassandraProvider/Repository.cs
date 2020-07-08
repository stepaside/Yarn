using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Yarn.Extensions;
using Yarn.Reflection;
using Yarn.Specification;
using Mapper = Cassandra.Mapping.Mapper;

namespace Yarn.Data.CassandraProvider
{
    public class Repository : IRepository, IMetaDataProvider
    {
        private Lazy<DataContext> _dataContext;
        private readonly string _connectionString;
        private readonly MappingConfiguration _configuration;

        public Repository(string connectionString = null, MappingConfiguration configuration = null)
        {
            _connectionString = connectionString;
            _configuration = configuration ?? MappingConfiguration.Global;
            _dataContext = new Lazy<DataContext>(CreateDataContext, true);
        }

        private DataContext CreateDataContext()
        {
            return new DataContext(_connectionString);
        }

        public IDataContext DataContext
        {
            get { return _dataContext.Value; }
        }

        protected ISession Session
        {
            get { return ((IDataContext<ISession>)DataContext).Session; }
        }

        protected Mapper GetMapper()
        {
            return new Mapper(Session, _configuration);
        }

        public T Add<T>(T entity) where T : class
        {
            var mapper = GetMapper();
            var result = mapper.InsertIfNotExists(entity);
            return result.Applied ? entity : result.Existing;
        }

        public IQueryable<T> All<T>() where T : class
        {
            return Session.GetTable<T>();
        }

        public long Count<T>() where T : class
        {
            return Session.GetTable<T>().LongCount();
        }

        public long Count<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Session.GetTable<T>().LongCount(criteria);
        }

        public long Count<T>(ISpecification<T> criteria) where T : class
        {
            return Count(((Specification<T>)criteria).Predicate);
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
                if (_dataContext != null && _dataContext.IsValueCreated)
                {
                    _dataContext.Value.Dispose();
                    _dataContext = null;
                }
            }
        }

        public IList<T> Execute<T>(string command, ParamList parameters) where T : class
        {
            var mapper = GetMapper();
            if (parameters != null && parameters.Count > 0)
            {
                return mapper.Fetch<T>(command, parameters.Values.ToArray()).ToList();
            }
            return mapper.Fetch<T>(command).ToList();
        }

        public T Find<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return Session.GetTable<T>().FirstOrDefault(criteria).Execute();
        }

        public T Find<T>(ISpecification<T> criteria) where T : class
        {
            return Find(((Specification<T>)criteria).Predicate);
        }

        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = Session.GetTable<T>().Where(criteria);
            var pagedQuery = this.Page(query, offset, limit, orderBy);
            var cql = pagedQuery as CqlQuery<T>;
            return cql != null ? cql.Execute() : pagedQuery.AsEnumerable();
        }

        public IEnumerable<T> FindAll<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return FindAll(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public T GetById<T, TKey>(TKey id) where T : class
        {
            var findById = this.BuildPrimaryKeyExpression<T, TKey>(id);
            return Find(findById);
        }

        public T Remove<T>(T entity) where T : class
        {
            var mapper = GetMapper();
            mapper.Delete(entity);
            return entity;
        }

        public T Remove<T, TKey>(TKey id) where T : class
        {
            var findById = this.BuildPrimaryKeyExpression<T, TKey>(id);
            var result = Session.GetTable<T>().Where(findById).DeleteIf(findById).Execute();
            return result.Applied ? result.Existing : null;
        }

        public T Update<T>(T entity) where T : class
        {
            var mapper = new Mapper(Session);
            mapper.Update(entity);
            return entity;
        }

        string[] IMetaDataProvider.GetPrimaryKey<T>()
        {
            var table = Session.GetTable<T>().GetTable();
            var metaData = Session.Cluster.Metadata.GetTable(table.KeyspaceName, table.Name);
            return metaData.PartitionKeys.Select(k => k.Name).Concat(metaData.ClusteringKeys.Select(t => t.Item1.Name)).Distinct().ToArray();
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
    }
}
