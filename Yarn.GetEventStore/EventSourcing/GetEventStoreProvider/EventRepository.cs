using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yarn.EventSourcing;

namespace Yarn.EventSourcing.GetEventStoreProvider
{
    public class EventRepository : IEventRepositoryAsync
    {
        private readonly IAggregateFactory _factory;

        private readonly IEventStoreConnection _connection;

        public EventRepository(IAggregateFactory factory, string connectionString)
        {
            _factory = factory;
            _connection = EventStoreConnection.Create(connectionString);
        }

        public void Dispose()
        {
            _connection.Dispose();
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
            var list = new List<object>();
            var start = 0L;
            var streamName = GetStreamName<T>(id);
            StreamEventsSlice streamEventsSlice;
            do
            {
                streamEventsSlice = await _connection.ReadStreamEventsForwardAsync(streamName, start, 200, false);
                start = streamEventsSlice.NextEventNumber;
                list.AddRange(streamEventsSlice.Events.Select(ConvertEvent));
            }
            while (!streamEventsSlice.IsEndOfStream);
            return _factory.Create<T>(list);
        }

        public async Task SaveAsync<T>(T aggregate) where T : class, IAggregate
        {
            var guid = Guid.NewGuid();
            var array = aggregate.GetUncommittedEvents().ToArray();
            if (array.Any())
            {
                var streamName = GetStreamName(aggregate.GetType(), aggregate.Id);
                var num = aggregate.Version - array.Length;
                var expectedVersion = (num == 0) ? -1 : (num - 1);
                var commitHeaders = new Dictionary<string, object>
                {
                    {
                        "CommitId",
                        guid
                    },
                    {
                        "AggregateClrType",
                        aggregate.GetType().AssemblyQualifiedName
                    }
                };
                var events = array.Select(e => ToEventData(e, commitHeaders)).ToList();
                await _connection.AppendToStreamAsync(streamName, expectedVersion, events);
                aggregate.ClearUncommittedEvents();
            }
        }

        private static string GetStreamName<T>(Guid id)
        {
            return GetStreamName(typeof(T), id);
        }

        private static string GetStreamName(Type type, Guid id)
        {
            return string.Format("{0}-{1}", type.Name, id);
        }

        private static object ConvertEvent(ResolvedEvent x)
        {
            var value = JObject.Parse(Encoding.UTF8.GetString(x.OriginalEvent.Metadata)).Property("EventClrType").Value;
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(x.OriginalEvent.Data), Type.GetType((string)value));
        }

        private static EventData ToEventData(object message, IDictionary<string, object> headers)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, settings));
            var value = new Dictionary<string, object>(headers)
            {
                {
                    "EventClrType",
                    message.GetType().AssemblyQualifiedName
                }
            };
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, settings));
            var name = message.GetType().Name;
            return new EventData(Guid.NewGuid(), name, true, data, metadata);
        }
    }
}
