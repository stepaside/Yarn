using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Yarn.Extensions;
using Yarn.Reflection;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class RepositoryAsync : Repository, IRepositoryAsync
    {
        public RepositoryAsync(bool lazyLoadingEnabled = true,
            bool proxyCreationEnabled = true,
            bool autoDetectChangesEnabled = false,
            bool validateOnSaveEnabled = true,
            bool migrationEnabled = false,
            string nameOrConnectionString = null,
            string assemblyNameOrLocation = null,
            Assembly configurationAssembly = null,
            Type dbContextType = null,
            bool mergeOnUpdate = false,
            DataContextLifeCycle lifeCycle = DataContextLifeCycle.DataContextCache)
            : base(
                lazyLoadingEnabled, proxyCreationEnabled, autoDetectChangesEnabled, validateOnSaveEnabled,
                migrationEnabled, nameOrConnectionString, assemblyNameOrLocation, configurationAssembly, dbContextType, mergeOnUpdate, lifeCycle)
        {
        }

        public async Task<T> GetByIdAsync<T, TKey>(TKey id) where T : class
        {
            return await Table<T>().FindAsync(id);
        }

        public async Task<T> FindAsync<T>(ISpecification<T> criteria) where T : class
        {
            return await FindAll<T>(criteria).AsQueryable<T>().FirstOrDefaultAsync();
        }

        public async Task<T> FindAsync<T>(Expression<Func<T, bool>> criteria) where T : class
        {
            return await Table<T>().FirstOrDefaultAsync(criteria);
        }

        public async Task<IEnumerable<T>> FindAllAsync<T>(ISpecification<T> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
        {
            var query = criteria.Apply(Table<T>());
            query = this.Page<T>(query, offset, limit, orderBy);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync<T>(Expression<Func<T, bool>> criteria, int offset = 0, int limit = 0, Sorting<T> orderBy = null) where T : class
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
                if (Context == null)
                {
                    Context = new DataContextAsync(LazyLoadingEnabled, ProxyCreationEnabled,
                        AutoDetectChangesEnabled, ValidateOnSaveEnabled, MigrationEnabled, NameOrConnectionString,
                        AssemblyNameOrLocation, ConfigurationAssembly, DbContextType, LifeCycle);
                }
                return (IDataContextAsync)Context;
            }
        }
    }
}
