using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using OR10N.Base;
using OR10N.ViewModel;
using UnityFlow;

namespace OR10N.Nodes
{
    /// <summary>
    /// Interaction logic for VariableNode.xaml
    /// </summary>
    public partial class VariableNode : NodeViewModel
    {
        private string _type;
        public string Type
        {
            get => _type;
            set
            {
                string previousType = _type;
                _type = value;
                OnPropertyChanged("Type");
                MainViewModel.LogStatus($"VariableNode Type changed from {previousType} to {_type} for node {NodeName}");
            }
        }

        public VariableKind KindOfVariable;

        public object TypeValue { get; set; }

        public bool IsInitialised => TypeValue != null;

        public VariableNode(MainViewModel mainViewModel) : base(mainViewModel)
        {
            MainViewModel.LogStatus("Initializing VariableNode...");
            try
            {
                InitializeComponent();
                MainViewModel.LogStatus("Component initialized for VariableNode.");

                this.Type = "string";
                MainViewModel.LogStatus($"Default Type set to: {Type}");

                this.NodeType = NodeType.VariableNode;
                MainViewModel.LogStatus($"NodeType set to: {NodeType}");

                this.NodeParameterOut.ParentNode = this;
                this.NodeParameterOut.TypeOfInputOutput = InputOutputType.Output;
                this.NodeParameterOut.TypeOfConnector = ConnectorType.VariableConnector;
                MainViewModel.LogStatus("NodeParameterOut configured for VariableNode.");

                KindOfVariable = VariableKind.Field;
                MainViewModel.LogStatus($"KindOfVariable set to: {KindOfVariable}");

                DataContext = this;
                MainViewModel.LogStatus("DataContext set for VariableNode.");
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error during VariableNode initialization: {ex.Message}", true);
            }
            MainViewModel.LogStatus("VariableNode initialization completed.");
        }

        public override void Populate(SerializeableNodeViewModel node)
        {
            MainViewModel.LogStatus($"Attempting to populate VariableNode with data from SerializeableNodeViewModel: {node?.NodeName}");
            try
            {
                base.Populate(node);
                if (node is SerializeableVariableNode v)
                {
                    this.Type = v.TypeString;
                    MainViewModel.LogStatus($"Type set to: {v.TypeString} for VariableNode {NodeName}");

                    this.CallingClass = node.CallingClass;
                    MainViewModel.LogStatus($"CallingClass set to: {node.CallingClass} for VariableNode {NodeName}");
                }
                else
                {
                    MainViewModel.LogStatus("Provided node is not of type SerializeableVariableNode.", true);
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error during VariableNode population: {ex.Message}", true);
            }
            MainViewModel.LogStatus("VariableNode population completed.");
        }

        public override string ToString()
        {
            MainViewModel.LogStatus($"ToString called for VariableNode: {NodeName}");
            return this.NodeName;
        }
    }
}
