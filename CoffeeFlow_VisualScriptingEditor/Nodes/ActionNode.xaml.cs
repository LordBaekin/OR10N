using OR10N.Base;
using OR10N.ViewModel;

namespace OR10N.Nodes
{
    public partial class ActionNode : NodeViewModel
    {
        public string Action { get; set; }
        public string DialogueText { get; set; }

        // Modify constructor to accept a MainViewModel and pass it to the base class
        public ActionNode(MainViewModel mainViewModel, string action, string dialogueText = "")
            : base(mainViewModel)  // Pass MainViewModel to base class
        {
            Action = action;
            DialogueText = dialogueText;
            NodeName = $"Action: {action}";
        }

        public override string GetSerializationString()
        {
            return $"ActionNode: {Action}";
        }

        // Additional logic for connecting nodes, serialization, etc.
    }
}
