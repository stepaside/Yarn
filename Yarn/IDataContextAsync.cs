using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IDataContextAsync : IDataContext
    {
        Task SaveChangesAsync();
    }

    public interface IDataContextAsync<TSession> : IDataContextAsync
    {
        TSession Session { get; }
    }
}
