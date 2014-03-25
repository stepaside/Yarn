using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.ModelConfiguration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarn.Data.EntityFrameworkProvider.Migrations;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DataContextAsync : DataContext, IDataContextAsync<DbContext>
    {
        public DataContextAsync() : this(prefix: null) { }

        public DataContextAsync(string prefix = null,
            bool lazyLoadingEnabled = true,
            bool proxyCreationEnabled = false,
            bool autoDetectChangesEnabled = false,
            bool validateOnSaveEnabled = true,
            bool migrationEnabled = false)
            : base(
                prefix, lazyLoadingEnabled, proxyCreationEnabled, autoDetectChangesEnabled, validateOnSaveEnabled,
                migrationEnabled)
        {

        }

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
