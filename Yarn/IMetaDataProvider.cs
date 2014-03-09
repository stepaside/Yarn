using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IMetaDataProvider
    {
        string[] GetPrimaryKey<T>() where T : class;
        object[] GetPrimaryKeyValue<T>(T entity) where T : class;
    }
}
