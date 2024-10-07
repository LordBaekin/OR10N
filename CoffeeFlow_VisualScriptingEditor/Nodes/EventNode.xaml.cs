using CoffeeFlow.Base;

namespace CoffeeFlow.Nodes
{
    public partial class EventNode : NodeViewModel
    {
        public string EventType { get; set; }

        public EventNode(string eventType)
        {
            EventType = eventType;
            NodeName = $"Event: {eventType}";
        }

        public override string GetSerializationString()
        {
            return $"EventNode: {EventType}";
        }

        // Additional logic for connecting nodes, serialization, etc.
    }
}
