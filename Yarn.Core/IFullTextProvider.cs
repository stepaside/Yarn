using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exsage.Core
{
    public interface IFullTextProvider
    {
        IList<T> Search<T>(string query) where T : class;
    }
}
