using OR10N.Base;
using OR10N.ViewModel;
using System;

namespace OR10N.Nodes
{
    public partial class EventNode : NodeViewModel
    {
        public string EventType { get; set; }

        // Modify constructor to accept MainViewModel and pass it to the base class
        public EventNode(MainViewModel mainViewModel, string eventType)
            : base(mainViewModel)  // Pass MainViewModel to base class
        {
            MainViewModel.LogStatus("Initializing EventNode...");
            try
            {
                EventType = eventType;
                MainViewModel.LogStatus($"EventType set to: {EventType}");

                NodeName = $"Event: {eventType}";
                MainViewModel.LogStatus($"NodeName set to: {NodeName}");

                MainViewModel.LogStatus("EventNode initialization completed.");
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error during EventNode initialization: {ex.Message}", true);
            }
        }

        public override string GetSerializationString()
        {
            MainViewModel.LogStatus($"Generating serialization string for EventNode: {NodeName}");
            string serializationString = $"EventNode: {EventType}";
            MainViewModel.LogStatus($"Serialization string generated: {serializationString}");
            return serializationString;
        }

        // Additional logic for connecting nodes, serialization, etc.
    }
}
