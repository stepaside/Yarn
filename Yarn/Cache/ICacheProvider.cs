using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;

namespace Yarn.Cache
{
    public interface ICacheProvider : IDisposable
    {
        T Get<T>(string key) where T : class;
        bool Set<T>(string key, T value) where T : class;
        T Remove<T>(string key) where T : class;
        int Clear(params string[] key);
        void ClearAll();
        CacheItemPolicy CachePolicy { get; }
    }
}
