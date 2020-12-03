using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Data.EntityFrameworkProvider.Migrations
{
    internal class ScriptGeneratorMigrationInitializer<TContext> : IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        public string CreateSql { get; private set; }

        public string UpdateSql { get; private set; }

        public bool Enabled { get; set; }

        public void InitializeDatabase(TContext context)
        {
            if (Enabled)
            {
                if (!context.Database.Exists() || !context.Database.CompatibleWithModel(false))
                {
                    var configuration = new DbMigrationsConfiguration<TContext>();
                    var migrator = new DbMigrator(configuration);
                    migrator.Configuration.TargetDatabase = new DbConnectionInfo(context.Database.Connection.ConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(context.Database.Connection.ConnectionString, null));

                    var migrations = migrator.GetPendingMigrations();
                    if (migrations.Any())
                    {
                        var scriptor = new MigratorScriptingDecorator(migrator);
                        CreateSql = scriptor.ScriptUpdate(null, migrations.Last());
                        UpdateSql = scriptor.ScriptUpdate(migrations.Last(), null);
                    }
                }
            }
        }
    }
}
