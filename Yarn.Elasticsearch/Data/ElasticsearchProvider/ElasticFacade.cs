using System;
using ElasticLinq;
using Elasticsearch.Net;
using Nest;

namespace Yarn.Elasticsearch.Data.ElasticsearchProvider
{
    public class ElasticFacade : IDisposable
    {
        internal ElasticFacade(ElasticContext linqClient, ElasticClient client)
        {
            LinqClient = linqClient;
            Client = client;
        }

        public ElasticContext LinqClient { get; private set; }

        public ElasticClient Client { get; private set; }

        public void Dispose()
        {
            var connection = LinqClient.Connection as ElasticConnection;
            if (connection != null)
            {
                connection.Dispose();
            }
        }
    }
}