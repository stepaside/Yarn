using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContextAsync : DataContext, IDataContextAsync<DbConnection>
    {
        public DataContextAsync(string connectionName = null, string connectionString = null, DbTransaction transaction = null) 
            : base(connectionName, connectionString, transaction)
        {
        }

        public async Task SaveChangesAsync()
        {
            await Task.Factory.StartNew(SaveChanges).ConfigureAwait(false);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(SaveChanges, cancellationToken).ConfigureAwait(false);
        }
    }
}
