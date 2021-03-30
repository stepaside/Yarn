using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nemo;
using Nemo.Data;
using Nemo.Serialization;
using Nemo.Utilities;

namespace Yarn.EventSourcing.NemoProvider
{
    public class SqlServerRepository : IEventRepositoryAsync
    {
        private readonly IAggregateFactory _factory;

        private readonly string _connectionString;

        private const string RetrieveByIdSql = "SELECT * FROM Events WHERE AggregateId = @id";

        private const string RetrieveMaxVersionSql = "SELECT MAX(Version) FROM Events WHERE AggregateId = @id";

        public SqlServerRepository(IAggregateFactory factory, string connectionString)
        {
            _factory = factory;
            _connectionString = connectionString;
        }

        public void Dispose()
        {
        }

        public T GetById<T>(Guid id) where T : class, IAggregate
        {
            return GetByIdAsync<T>(id).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Save<T>(T aggregate) where T : class, IAggregate
        {
            SaveAsync(aggregate).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<T> GetByIdAsync<T>(Guid id) where T : class, IAggregate
        {
            var events = (await ObjectFactory.RetrieveAsync<EventData>(sql: RetrieveByIdSql, parameters: new[] { new Param { Name = "id", Value = id } }, connection: DbFactory.CreateConnection(_connectionString))).Select(ConvertEvent);
            return _factory.Create<T>(events);
        }

        public async Task SaveAsync<T>(T aggregate) where T : class, IAggregate
        {
            var array = aggregate.GetUncommittedEvents().ToArray();
            if (array.Any())
            {
                var aggregateType = aggregate.GetType().Name;
                var originalVersion = aggregate.Version - array.Length + 1;
                var eventData = array.Select(e => ToEventData(e, aggregateType, aggregate.Id, originalVersion++));
                using (var transaction = ObjectFactory.CreateTransactionScope())
                {
                    using (var connection = DbFactory.CreateConnection(_connectionString))
                    {
                        connection.Open();
                        var num = await ObjectFactory.RetrieveScalarAsync<int>(RetrieveMaxVersionSql, new[] { new Param { Name = "id", Value = aggregate.Id } }, connection: connection);
                        if (num >= originalVersion)
                        {
                            throw new Exception("Concurrency exception");
                        }
                        await ObjectFactory.InsertAsync(eventData, connection: connection);
                        transaction.Complete();

                        aggregate.ClearUncommittedEvents();
                    }
                }
            }
        }

        private static object ConvertEvent(EventData x)
        {
            var metadata = Json.Parse(x.Metadata);
            var crlType = metadata["EventClrType"];
            var typeName = crlType != null ? (string)crlType.Value : null;
            return typeName != null ? x.Event.FromJson(Type.GetType(typeName)) : null;
        }

        private static EventData ToEventData(object eventItem, string aggregateType, Guid aggregateId, int version)
        {
            var eventJson = eventItem.ToJson();
            var metadata = new Dictionary<string, object>
            {
                {
                    "EventClrType",
                    eventItem.GetType().AssemblyQualifiedName
                }
            }.ToJson();
            var id = CombGuid.Generate();
            return new EventData
            {
                Id = id,
                Created = DateTime.UtcNow,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                Version = version,
                Event = eventJson,
                Metadata = metadata
            };
        }
    }
}
