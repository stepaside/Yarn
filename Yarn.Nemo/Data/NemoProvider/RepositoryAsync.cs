using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Nemo;
using Nemo.Configuration;
using Yarn.Extensions;
using Yarn.Specification;

namespace Yarn.Data.NemoProvider
{
    public class RepositoryAsync : Repository, IRepositoryAsync
    {
        private readonly IDataContextAsync _context;

        public RepositoryAsync(bool useStoredProcedures, IConfiguration configuration = null, string connectionName = null, string connectionString = null, DbTransaction transaction = null) :
            base(useStoredProcedures, configuration, connectionName, connectionString, transaction)
        {
            _context = new DataContextAsync(configuration != null ? configuration.DefaultConnectionName : connectionName, connectionString, transaction);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        IDataContextAsync IRepositoryAsync.DataContext
        {
            get { return _context; }
        }

        public async Task<long> CountAsync<T>() where T : class
        {
            return await ObjectFactory.CountAsync<T>(connection: Connection);
        }

        public Task<long> CountAsync<T>(ISpecification<T> criteria) where T : class
        {
            return CountAsync(((Specification<T>)criteria).Predicate);
        }

        public async Task<long> CountAsync<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return await ObjectFactory.CountAsync(criteria, connection: Connection);
        }

        public async Task<IList<T>> ExecuteAsync<T>(string command, ParamList parameters) where T : class
        {
            var response = await ObjectFactory.ExecuteAsync<T>(new OperationRequest { Operation = command, OperationType = OperationType.Guess, Parameters = parameters != null ? parameters.Select(p => new Param { Name = p.Key, Value = p.Value }).ToArray() : null, Connection = Connection, Transaction = ((DataContext)DataContext).Transaction });
            return ObjectFactory.Translate<T>(response).ToList();
        }

        public Task<IEnumerable<T>> FindAllAsync<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            return FindAllAsync(((Specification<T>)criteria).Predicate, offset, limit, orderBy);
        }

        public Task<IEnumerable<T>> FindAllAsync<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            if (orderBy != null)
            {
                return ObjectFactory.SelectAsync(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit, orderBy: new Nemo.Sorting<T> { OrderBy = orderBy.OrderBy, Reverse = orderBy.Reverse }).ToEnumerableAsync();
            }
            return ObjectFactory.SelectAsync(criteria, connection: Connection, page: limit > 0 ? offset / limit + 1 : 0, pageSize: limit).ToEnumerableAsync();
        }

        public Task<T> FindAsync<T>(ISpecification<T> criteria) where T : class
        {
            return FindAsync(((Specification<T>)criteria).Predicate);
        }

        public async Task<T> FindAsync<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return (await FindAllAsync(criteria, limit: 1)).FirstOrDefault();
        }

        public async Task<T> GetByIdAsync<T, TKey>(TKey id) where T : class
        {
            var property = GetPrimaryKey<T>().First();
            return _useStoredProcedures
                ? (await ObjectFactory.RetrieveAsync<T>("GetById", parameters: new[] { new Param { Name = property, Value = id } }, connection: Connection)).FirstOrDefault()
                : await ObjectFactory.SelectAsync(this.BuildPrimaryKeyExpression<T, TKey>(id), connection: Connection).FirstOrDefault();
        }
    }
}
