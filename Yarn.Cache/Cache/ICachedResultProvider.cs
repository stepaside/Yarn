using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;

namespace Yarn.Cache
{
    public interface ICachedResultProvider : IDisposable
    {
        T Get<T>(string key) where T : class;
        bool Put<T>(string key, T value) where T : class;
        T Remove<T>(string key) where T : class;
        int Evict(params string[] keys);
        void Reset();
        CacheItemPolicy CachePolicy { get; }
    }
}
