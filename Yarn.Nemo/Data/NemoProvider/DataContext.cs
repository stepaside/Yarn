using System.Data.Common;
using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContext : IDataContext<DbConnection>
    {
        public DataContext(DataContextOptions options)
            : this(options, null)
        { }

        public DataContext(DataContextOptions options, DbTransaction transaction)
        {
            Options = options;
            Transaction = transaction;
            if (transaction != null)
            {
                Session = transaction.Connection;
            }
            else if (options?.ConnectionName != null)
            {
                Session = DbFactory.CreateConnection(options.ConnectionName, GetConfiguration(options));
            }
            else if (options?.ConnectionString != null)
            {
                Session = DbFactory.CreateConnection(options.ConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(options.ConnectionString, GetConfiguration(options)));
            }
            else
            {
                Session = DbFactory.CreateConnection(ConfigurationFactory.DefaultConnectionName, GetConfiguration(options));
            }
            Source = Session?.ConnectionString;
        }


        private static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(DataContextOptions options)
        {
#if NETSTANDARD
            return (options.Configuration ?? ConfigurationFactory.DefaultConfiguration).SystemConfiguration;
#else
            return null;
#endif
        }

        internal DataContextOptions Options { get; }

        public void SaveChanges()
        {
            if (Transaction == null)
            {
                return;
            }

            try
            {
                Transaction.Commit();
            }
            catch
            {
                Transaction.Rollback();
            }
        }

        public string Source { get; }

        public DbTransaction Transaction { get; }

        public void Dispose()
        {
            Session?.Dispose();
        }

        public DbConnection Session { get; }
    }
}
