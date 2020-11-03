using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Yarn.Extensions;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public static class DataContextExtensions
    {
        private readonly static ConcurrentDictionary<Type, Delegate> _pkCache = new ConcurrentDictionary<Type, Delegate>();

        private static IEnumerable<Tuple<string, object>> GetPrimaryKey<T>(T entity, DbContext dbContext) where T : class
        {
            return MetaDataProvider.Current.GetPrimaryKey<T>(dbContext).Zip(MetaDataProvider.Current.GetPrimaryKeyValue(entity, dbContext), Tuple.Create);
        }

        public static void Attach<T>(this IDataContext context, T entity) where T : class
        {
            if (entity == null) return;

            if (!(context is IDataContext<DbContext> dbContext)) return;

            var entry = dbContext.Session.Entry(entity);
            var dbSet = dbContext.Session.Set<T>();
            if (entry == null)
            {
                dbSet.Attach(entity);
            }
            else if (entry.State == EntityState.Detached)
            {
                var findByPrimaryKey = (Func<T, bool>)_pkCache.GetOrAdd(typeof(T), t => entity.BuildPrimaryKeyExpression(e => GetPrimaryKey(e, dbContext.Session)).Compile());
                var attachedEntity = dbSet.Local.FirstOrDefault(findByPrimaryKey);
                if (attachedEntity != null)
                {
                    entry.State = EntityState.Unchanged;
                }
                else
                {
                    dbSet.Attach(entity);
                }
            }
        }

        public static void Detach<T>(this IDataContext context, T entity) where T : class
        {
            if (entity == null) return;

            if (!(context is IDataContext<DbContext> dbContext)) return;

            dbContext.Session.Entry(entity).State = EntityState.Detached;
        }
    }
}
