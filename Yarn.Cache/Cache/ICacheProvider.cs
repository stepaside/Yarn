using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;

namespace Yarn.Cache
{
    public interface ICacheProvider : IDisposable
    {
        bool Get<T>(string key, out T item);
        bool Add<T>(string key, T value);
        void Set<T>(string key, T value);
        bool Exists(string key);
        bool Remove(string key);
        void Reset();
        uint Increment(string key, uint initial = 1, uint delta = 1);
        CacheItemPolicy CachePolicy { get; }
    }
}
