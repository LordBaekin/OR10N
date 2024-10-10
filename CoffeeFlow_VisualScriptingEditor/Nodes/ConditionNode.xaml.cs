using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using GalaSoft.MvvmLight.CommandWpf;
using System.Xml.Serialization;
using UnityFlow;
using OR10N.ViewModel;

namespace OR10N.Nodes
{
    /// <summary>
    /// Interaction logic for DynamicNode.xaml
    /// </summary>
    partial class ConditionNode : NodeViewModel
    {
        public string FullString;
        public string ConnectedToVariableName { get; set; }
        public string ConnectedToVariableCallerClassName { get; set; }

        public ConditionNode(MainViewModel mainViewModel) : base(mainViewModel)
        {
            MainViewModel.LogStatus("Initializing ConditionNode...");
            InitializeComponent();

            this.NodeType = NodeType.ConditionNode;
            MainViewModel.LogStatus($"NodeType set to {NodeType} for node {NodeName}");

            InExecutionConnector.ParentNode = this;
            InExecutionConnector.TypeOfInputOutput = InputOutputType.Input;
            MainViewModel.LogStatus("Configured InExecutionConnector as Input");

            OutExecutionConnectorTrue.ParentNode = this;
            OutExecutionConnectorTrue.TypeOfInputOutput = InputOutputType.Output;
            MainViewModel.LogStatus("Configured OutExecutionConnectorTrue as Output");

            OutExecutionConnectorFalse.ParentNode = this;
            OutExecutionConnectorFalse.TypeOfInputOutput = InputOutputType.Output;
            MainViewModel.LogStatus("Configured OutExecutionConnectorFalse as Output");

            boolInput.ParentNode = this;
            boolInput.TypeOfInputOutput = InputOutputType.Input;
            MainViewModel.LogStatus("Configured boolInput as Input");

            DataContext = this;
            MainViewModel.LogStatus("ConditionNode DataContext set and initialization completed.");
        }

        public override void Populate(SerializeableNodeViewModel node)
        {
            MainViewModel.LogStatus($"Populating ConditionNode with data from serialized node: {node.NodeName}");
            base.Populate(node);

            try
            {
                SerializeableConditionNode ser = (node as SerializeableConditionNode);
                this.InExecutionConnector.ConnectionNodeID = ser.InputNodeID;
                this.OutExecutionConnectorFalse.ConnectionNodeID = ser.OutputFalseNodeID;
                this.OutExecutionConnectorTrue.ConnectionNodeID = ser.OutputTrueNodeID;
                this.boolInput.ConnectionNodeID = ser.BoolVariableID;
                this.ConnectedToVariableCallerClassName = ser.BoolCallingClass;

                this.CallingClass = node.CallingClass;

                MainViewModel.LogStatus($"ConditionNode populated with InputNodeID: {ser.InputNodeID}, " +
                    $"OutputTrueNodeID: {ser.OutputTrueNodeID}, OutputFalseNodeID: {ser.OutputFalseNodeID}, " +
                    $"BoolVariableID: {ser.BoolVariableID}, CallingClass: {node.CallingClass}");
            }
            catch (Exception ex)
            {
                MainViewModel.LogStatus($"Error while populating ConditionNode: {ex.Message}");
            }
        }

        public override string ToString()
        {
            MainViewModel.LogStatus($"ToString called on ConditionNode: {NodeName}");
            return NodeName.ToString();
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            MainViewModel.LogStatus($"Searching for child of type {typeof(childItem)} in visual tree of ConditionNode: {NodeName}");
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                {
                    MainViewModel.LogStatus($"Found child of type {typeof(childItem)} in ConditionNode: {NodeName}");
                    return (childItem)child;
                }
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                    {
                        MainViewModel.LogStatus($"Found child of type {typeof(childItem)} in ConditionNode: {NodeName}");
                        return childOfChild;
                    }
                }
            }
            MainViewModel.LogStatus($"No child of type {typeof(childItem)} found in visual tree of ConditionNode: {NodeName}");
            return null;
        }
    }
}
