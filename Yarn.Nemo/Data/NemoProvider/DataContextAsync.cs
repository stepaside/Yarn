using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Nemo.Configuration;

namespace Yarn.Data.NemoProvider
{
    public class DataContextAsync : DataContext, IDataContextAsync<DbConnection>
    {
        public DataContextAsync(DataContextOptions options)
            : this(options, null)
        { }

        public DataContextAsync(DataContextOptions options, DbTransaction transaction)
            : base(options, transaction)
        { }

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
