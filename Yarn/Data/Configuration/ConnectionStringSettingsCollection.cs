using System;
using System.Collections;
using System.Collections.Generic;

namespace Yarn.Data.Configuration
{
    internal class ConnectionStringSettingsCollection : List<ConnectionStringSettings>
    {
        public ConnectionStringSettingsCollection()
        {
        }

        public ConnectionStringSettingsCollection(int capacity) : base(capacity)
        {
        }

        public ConnectionStringSettingsCollection(IEnumerable<ConnectionStringSettings> collection) : base(collection)
        {
        }

        public bool TryGetValue(string key, out ConnectionStringSettings value)
        {
            var found = Find(m => m.Name == key);
            value = found;
            return found != null;
        }
    }
}