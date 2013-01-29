using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IMetaDataProvider
    {
        IEnumerable<string> GetPrimaryKey<T>() where T : class;
        IDictionary<string, object> GetPrimaryKeyValue<T>(T entity) where T : class;
    }
}
