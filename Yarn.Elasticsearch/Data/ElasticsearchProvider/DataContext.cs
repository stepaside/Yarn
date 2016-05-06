using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElasticLinq;
using Elasticsearch.Net;
using Nest;

namespace Yarn.Elasticsearch.Data.ElasticsearchProvider
{
    public class DataContext : IDataContext<ElasticFacade>
    {
        private readonly ElasticFacade _facade;

        public DataContext(string connection, string userName = null, string password = null)
        {
            var client = new ElasticClient(new ConnectionSettings(new Uri(connection)));

            var linqClient = new ElasticContext(new ElasticConnection(new Uri(connection), userName, password));

            _facade = new ElasticFacade(linqClient, client);
        }

        public ElasticFacade Session
        {
            get { return _facade; }
        }

        public void SaveChanges()
        {
            
        }

        public string Source
        {
            get
            {
                var connection = _facade.LinqClient.Connection as ElasticConnection;
                return connection != null ? connection.Endpoint.ToString() : _facade.LinqClient.Connection.ToString();
            }
        }

        public void Dispose()
        {
            _facade.Dispose();
        }
    }
}
