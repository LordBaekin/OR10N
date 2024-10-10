using OR10N.Base;
using OR10N.ViewModel;

namespace OR10N.Nodes
{
    public partial class ActionNode : NodeViewModel
    {
        private string _action;
        public string Action
        {
            get => _action;
            set
            {
                var previousAction = _action;
                _action = value;
                OnPropertyChanged(nameof(Action));
                MainViewModel.LogStatus($"Action changed from '{previousAction}' to '{_action}' for node '{NodeName}'");

                // Update NodeName to reflect the new action
                NodeName = $"Action: {_action}";
            }
        }

        private string _dialogueText;
        public string DialogueText
        {
            get => _dialogueText;
            set
            {
                var previousText = _dialogueText;
                _dialogueText = value;
                OnPropertyChanged(nameof(DialogueText));
                MainViewModel.LogStatus($"DialogueText changed from '{previousText}' to '{_dialogueText}' for node '{NodeName}'");
            }
        }

        // Modify constructor to accept a MainViewModel and pass it to the base class
        public ActionNode(MainViewModel mainViewModel, string action, string dialogueText = "")
            : base(mainViewModel)  // Pass MainViewModel to base class
        {
            MainViewModel.LogStatus($"Creating ActionNode with action '{action}' and dialogueText '{dialogueText}'");

            Action = action;
            DialogueText = dialogueText;
            NodeName = $"Action: {action}";

            MainViewModel.LogStatus($"ActionNode created with NodeName '{NodeName}' and ID '{ID}'");
        }

        public override string GetSerializationString()
        {
            var serializationString = $"ActionNode: {Action}";
            MainViewModel.LogStatus($"Serialized ActionNode '{NodeName}' to '{serializationString}'");
            return serializationString;
        }

        // Additional logic for connecting nodes, serialization, etc.
    }
}
