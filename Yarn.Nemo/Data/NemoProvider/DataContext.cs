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
                _connection = DbFactory.CreateConnection(options.ConnectionName);
            }
            else if (options?.ConnectionString != null)
            {
                _connection = DbFactory.CreateConnection(options.ConnectionString, DbFactory.GetProviderInvariantNameByConnectionString(options.ConnectionString));
            }
            else
            {
                _connection = DbFactory.CreateConnection(ConfigurationFactory.DefaultConnectionName);
            }
            _source = options.ConnectionString;

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
