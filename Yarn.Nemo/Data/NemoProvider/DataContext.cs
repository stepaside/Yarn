using System.Data.Common;
using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContext : IDataContext<DbConnection>
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly string _source;

        public DataContext(DataContextOptions options)
            : this(options, null)
        { }

        public DataContext(DataContextOptions options, DbTransaction transaction)
        {
            _transaction = transaction;
            if (transaction != null)
            {
                _connection = transaction.Connection;
            }
            else if (options?.ConnectionName != null)
            {
                _connection = DbFactory.CreateConnection(options.ConnectionName, GetConfiguration(options));
            }
            else if (options?.ConnectionString != null)
            {
                _connection = DbFactory.CreateConnection(options.ConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(options.ConnectionString, GetConfiguration(options)));
            }
            else
            {
                _connection = DbFactory.CreateConnection(ConfigurationFactory.DefaultConnectionName, GetConfiguration(options));
            }
            _source = _connection?.ConnectionString;
        }

        private static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(DataContextOptions options)
        {
            return (options.Configuration ?? ConfigurationFactory.DefaultConfiguration).SystemConfiguration;
        }

        public void SaveChanges()
        {
            if (_transaction == null)
            {
                return;
            }

            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
            }
        }

        public string Source
        {
            get { return _source; }
        }

        public DbTransaction Transaction
        {
            get { return _transaction; }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        public DbConnection Session
        {
            get { return _connection; }
        }
    }
}
