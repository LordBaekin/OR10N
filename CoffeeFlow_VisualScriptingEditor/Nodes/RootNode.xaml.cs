using System;
using System.Collections.Generic;
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
using OR10N.Base;
using OR10N.ViewModel;
using UnityFlow;

namespace OR10N.Nodes
{
    /// <summary>
    /// Interaction logic for RootNode.xaml
    /// </summary>
    public partial class RootNode : NodeViewModel
    {
        public Connector OutputConnector;
        public string EventSourceNodeName;

        public RootNode(MainViewModel mainViewModel) : base(mainViewModel)
        {
            MainViewModel.LogStatus("Initializing RootNode...");
            try
            {
                InitializeComponent();
                MainViewModel.LogStatus("Component initialized for RootNode.");

                this.NodeName = "ROOTNODE";
                MainViewModel.LogStatus($"NodeName set to: {NodeName}");

                this.MainConnector.ParentNode = this;
                this.MainConnector.TypeOfInputOutput = InputOutputType.Output;
                MainViewModel.LogStatus("Configured MainConnector for RootNode.");

                OutputConnector = this.MainConnector;
                MainViewModel.LogStatus("OutputConnector assigned to MainConnector.");

                DataContext = this;
                MainViewModel.LogStatus("DataContext set for RootNode.");
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error during RootNode initialization: {ex.Message}", true);
            }
            MainViewModel.LogStatus("RootNode initialization completed.");
        }

        public MainViewModel mainViewModel { get; private set; }

        public RootNode GetCopy()
        {
            MainViewModel.LogStatus("Creating a copy of RootNode...");
            try
            {
                RootNode newNode = new RootNode(mainViewModel);
                newNode.NodeName = this.NodeName;
                MainViewModel.LogStatus($"New RootNode copy created with NodeName: {newNode.NodeName}");
                return newNode;
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error while creating a copy of RootNode: {ex.Message}", true);
                return null;
            }
        }

        public override void Populate(SerializeableNodeViewModel node)
        {
            MainViewModel.LogStatus($"Populating RootNode with data from SerializeableNodeViewModel: {node?.NodeName}");
            try
            {
                base.Populate(node);
                var serializedNode = node as SerializeableRootNode;
                if (serializedNode == null)
                {
                    MainViewModel.LogStatus("Failed to cast SerializeableNodeViewModel to SerializeableRootNode.", true);
                    return;
                }

                this.MainConnector.ConnectionNodeID = serializedNode.OutputNodeID;
                MainViewModel.LogStatus($"Set ConnectionNodeID for MainConnector to: {serializedNode.OutputNodeID}");
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error during RootNode population: {ex.Message}", true);
            }
            MainViewModel.LogStatus("RootNode population completed.");
        }
    }
}
