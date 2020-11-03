using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public class DataContextAsync : DataContext, IDataContextAsync<DbContext>
    {
        public DataContextAsync(DbContext dbContext)
            : base(dbContext)
        { }

        public async Task SaveChangesAsync()
        {
            await Session.SaveChangesAsync();
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await Session.SaveChangesAsync(cancellationToken);
        }
    }

    public class DataContextAsync<T> : DataContext<T>, IDataContextAsync<T>
        where T : DbContext
    {
        public DataContextAsync(T dbContext)
            : base(dbContext)
        { }

        public async Task SaveChangesAsync()
        {
            await Session.SaveChangesAsync();
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await Session.SaveChangesAsync(cancellationToken);
        }
    }
}
