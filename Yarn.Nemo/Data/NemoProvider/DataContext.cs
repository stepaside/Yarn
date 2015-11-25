using System.Data.Common;
using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContext : IDataContext<DbConnection>
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly string _source;

        public DataContext(string connectionName = null, string connectionString = null, DbTransaction transaction = null)
        {
            _transaction = transaction;
            if (transaction != null)
            {
                _connection = transaction.Connection;
            }
            else if (connectionName != null)
            {
                _connection = DbFactory.CreateConnection(connectionName);
                _source = _connection.ConnectionString;
            }
            else if (connectionString != null)
            {
                _connection = DbFactory.CreateConnection(connectionString, DbFactory.GetProviderInvariantNameByConnectionString(connectionString));
                _source = connectionString;
            }
            else
            {
                _connection = DbFactory.CreateConnection(ConfigurationFactory.DefaultConnectionName);
            }
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
