using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data;

namespace Yarn.Data.CassandraProvider
{
    public class DataContext : IDataContext<ISession>
    {
        private static readonly ConcurrentDictionary<string, ISession> Sessions = new ConcurrentDictionary<string, ISession>();

        public DataContext(string connectionString, string keySpace = null)
        {
            Session = Sessions.GetOrAdd(connectionString, cs =>
            {
                var cluster = Cluster.Builder().WithConnectionString(connectionString).Build();
                return keySpace == null ? cluster.Connect() : cluster.Connect(keySpace);
            });
            Source = connectionString;
            
        }

        public void Dispose()
        {
            
        }

        public void SaveChanges()
        {
            
        }
        
        public string Source { get; private set; }

        public ISession Session { get; private set; }
    }
}
