using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IDataContextAsync : IDataContext
    {
        Task SaveChangesAsync();
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }

    public interface IDataContextAsync<out TSession> : IDataContextAsync, IDataContext<TSession>
    {
    }
}
