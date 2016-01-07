namespace Yarn.EventSourcing
{
    public interface IEventRouter
    {
        void Invoke(IAggregate aggregate, object eventData);
    }
}