using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IFullTextProvider
    {
        IDataContext DataContext { get; set; }
        void Index<T>() where T : class;
        string Prepare<T>(string searchTerms) where T : class;
        IList<T> Search<T>(string query) where T : class;
    }
}
