using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exsage.Core
{
    public interface IDataContext
    {
        void SaveChanges();
        string Source { get; }
        IDataContextCache DataContextCache { get; }
    }

    public interface IDataContext<TSession> : IDataContext
    {
        TSession Session { get; }
    }
}
