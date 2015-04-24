using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarn.Adapters;

namespace Yarn.Extensions
{
    public static class RepositoryExtensions
    {
        public static IQueryable<T> Page<T>(this IRepository repository, IQueryable<T> query, int offset, int limit, Sorting<T> sorting)
            where T : class
        {
            if (offset > 0)
            {
                if (sorting == null)
                {
                    sorting = new Sorting<T>();
                }

                if (sorting.OrderBy == null)
                {
                    var primaryKey = ((IMetaDataProvider)repository).GetPrimaryKey<T>().First();
                    var parameter = Expression.Parameter(typeof(T));
                    var body = Expression.Convert(Expression.PropertyOrField(parameter, primaryKey), typeof(T).GetProperty(primaryKey).PropertyType);
                    sorting.OrderBy = Expression.Lambda<Func<T, object>>(body, parameter);
                }

                query = (sorting.Reverse ? query.OrderByDescending(sorting.OrderBy) : query.OrderBy(sorting.OrderBy)).Skip(offset);
            }
            if (limit > 0)
            {
                query = query.Take(limit);
            }
            return query;
        }

        public static IRepository WithSoftDelete(this IRepository repository, IPrincipal principal = null)
        {
            return new SoftDeleteRepository(repository, principal ?? Thread.CurrentPrincipal);
        }

        public static IRepository WithAudit(this IRepository repository, IPrincipal principal = null)
        {
            return new AuditableRepository(repository, principal ?? Thread.CurrentPrincipal);
        }

        public static IRepository WithMultiTenancy(this IRepository repository, ITenant tenant)
        {
            return new MultiTenantRepository(repository, tenant);
        }

        public static IRepository WithFailover(this IRepository repository, IRepository otherRepository, Action<Exception> logger = null, FailoverStrategy strategy = FailoverStrategy.ReplicationOnly)
        {
            return new FailoverRepostiory(repository, otherRepository, logger, strategy);
        }

        public static IRepository WithInterceptor(this IRepository repository, Func<InterceptorContext, IDisposable> interceptorFactory)
        {
            return new InterceptorRepository(repository, interceptorFactory);
        }

        public static T As<T>(this IRepository repository)
        {
            if (repository is T)
            {
                return (T)repository;
            }
            return default(T);
        }
    }
}
