using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IDataContext : IDisposable
    {
        void SaveChanges();
        string Source { get; }
    }

    public interface IDataContext<TSession> : IDataContext
    {
        TSession Session { get; }
    }
}
