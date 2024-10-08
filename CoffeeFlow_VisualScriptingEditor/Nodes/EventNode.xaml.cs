using CoffeeFlow.Base;
using CoffeeFlow.ViewModel;

namespace CoffeeFlow.Nodes
{
    public partial class EventNode : NodeViewModel
    {
        public string EventType { get; set; }

        // Modify constructor to accept MainViewModel and pass it to the base class
        public EventNode(MainViewModel mainViewModel, string eventType)
            : base(mainViewModel)  // Pass MainViewModel to base class
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
