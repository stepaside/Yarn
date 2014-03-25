using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Reflection;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class RepositoryAsync : Repository, IRepositoryAsync
    {
        public RepositoryAsync() : this(prefix: null) { }

        public RepositoryAsync(string prefix = null,
                            bool lazyLoadingEnabled = true,
                            bool proxyCreationEnabled = false,
                            bool autoDetectChangesEnabled = false,
                            bool validateOnSaveEnabled = true,
                            bool migrationEnabled = false)
            : base(prefix, lazyLoadingEnabled, proxyCreationEnabled, autoDetectChangesEnabled, validateOnSaveEnabled, migrationEnabled)
        { }

        public async Task<T> GetByIdAsync<T, ID>(ID id) where T : class
        {
            return await this.Table<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetByIdListAsync<T, ID>(IList<ID> ids) where T : class
        {
            return await base.GetByIdList<T, ID>(ids).AsQueryable<T>().ToListAsync();
        }

        public async Task<T> FindAsync<T>(ISpecification<T> criteria) where T : class
        {
            return await base.FindAll<T>(criteria).AsQueryable<T>().FirstOrDefaultAsync();
        }

        public new async Task<T> FindAsync<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return await this.Table<T>().FirstOrDefaultAsync(criteria);
        }

        public async Task<IEnumerable<T>> FindAllAsync<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = criteria.Apply(Table<T>());
            query = this.Page<T>(query, offset, limit, orderBy);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Expression<Func<T, object>> orderBy = null) where T : class
        {
            var query = this.Table<T>().Where(criteria);
            query = this.Page<T>(query, offset, limit, orderBy);
            return await query.ToListAsync();
        }

        public async Task<IList<T>> ExecuteAsync<T>(string command, ParamList parameters) where T : class
        {
            return await this.PrepareSqlQuery<T>(command, parameters).ToArrayAsync();
        }
        
        public async Task<long> CountAsync<T>() where T : class
        {
            return await this.Table<T>().LongCountAsync();
        }

        public async Task<long> CountAsync<T>(ISpecification<T> criteria) where T : class
        {
            return await FindAll<T>(criteria).AsQueryable<T>().LongCountAsync();
        }

        public async Task<long> CountAsync<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return await FindAll<T>(criteria).AsQueryable<T>().LongCountAsync();
        }

        public new IDataContextAsync DataContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new DataContextAsync(_prefix, _lazyLoadingEnabled, _proxyCreationEnabled, _autoDetectChangesEnabled, _validateOnSaveEnabled, _migrationEnabled);
                }
                return (IDataContextAsync)_context;
                
            }
        }
    }
}
