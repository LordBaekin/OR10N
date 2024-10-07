using CoffeeFlow.Base;

namespace CoffeeFlow.Nodes
{
    public partial class ActionNode : NodeViewModel
    {
        public string Action { get; set; }
        public string DialogueText { get; set; }

        public ActionNode(string action, string dialogueText = "")
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
