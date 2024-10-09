using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.CommandWpf;
using OR10N.Base;
using OR10N.Nodes;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UnityFlow;
using NLua;
using System.Diagnostics;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight.Ioc;


namespace OR10N.ViewModel
{
    /**********************************************************************************************************
   *             Logic related to the nodes present on the grid and their connections and related commands
   * 
   *                                                      * Nick @ http://immersivenick.wordpress.com 
   *                                                      * Free for non-commercial use
   * *********************************************************************************************************/
    public class NetworkViewModel
    {
        public Stack<RelayCommand> UndoStack;
        public List<NodeViewModel> AddedNodesOrder;
        public List<NodeViewModel> RemovedNodesOrder;
        public int UndoCount = 0;
        private NodeFactory nodeFactory;

        // Stacks for undo and redo actions
        private Stack<UndoableAction> undoStack = new Stack<UndoableAction>();
        private Stack<UndoableAction> redoStack = new Stack<UndoableAction>();

        // RelayCommands for Undo/Redo
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        // Get MainViewModel instance (you can adjust this to how you're managing the MainViewModel)
        MainViewModel mainViewModel = MainViewModel.Instance;
        public class UndoableAction
        {
            public Action DoAction { get; }
            public Action UndoAction { get; }

            public UndoableAction(Action doAction, Action undoAction)
            {
                MainViewModel.Instance.LogStatus("Creating UndoableAction.");
                DoAction = doAction;
                UndoAction = undoAction;
                MainViewModel.Instance.LogStatus("UndoableAction created.");
            }

            public void Execute()
            {
                MainViewModel.Instance.LogStatus("Executing UndoableAction.");
                DoAction();
            }

            public void Undo()
            {
                MainViewModel.Instance.LogStatus("Undoing action in UndoableAction.");
                UndoAction();
            }
        }

        private RelayCommand<NodeWrapper> _addNodeToGridCommand;
        public RelayCommand<NodeWrapper> AddNodeToGridCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing AddNodeToGridCommand...");
                if (_addNodeToGridCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing AddNodeToGridCommand...");
                    _addNodeToGridCommand = new RelayCommand<NodeWrapper>(AddNodeToGrid);
                    MainViewModel.Instance.LogStatus("AddNodeToGridCommand initialized.");
                }
                return _addNodeToGridCommand;
            }
        }

        private RelayCommand<LocalizationItem> _selectLocalizedStringCommand;
        public RelayCommand<LocalizationItem> SelectLocalizedStringCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing SelectLocalizedStringCommand...");
                if (_selectLocalizedStringCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing SelectLocalizedStringCommand...");
                    _selectLocalizedStringCommand = new RelayCommand<LocalizationItem>(SelectLocalizedString);
                    MainViewModel.Instance.LogStatus("SelectLocalizedStringCommand initialized.");
                }
                return _selectLocalizedStringCommand;
            }
        }

        private RelayCommand _SaveNodesCommand;
        public RelayCommand SaveNodesCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing SaveNodesCommand...");
                if (_SaveNodesCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing SaveNodesCommand...");
                    _SaveNodesCommand = new RelayCommand(SaveNodes);
                    MainViewModel.Instance.LogStatus("SaveNodesCommand initialized.");
                }
                return _SaveNodesCommand;
            }
        }

        private RelayCommand _ClearNodesCommand;
        public RelayCommand ClearNodesCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing ClearNodesCommand...");
                if (_ClearNodesCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing ClearNodesCommand...");
                    _ClearNodesCommand = new RelayCommand(ClearNodes);
                    MainViewModel.Instance.LogStatus("ClearNodesCommand initialized.");
                }
                return _ClearNodesCommand;
            }
        }

        private RelayCommand _LoadNodesCommand;
        public RelayCommand LoadNodesCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing LoadNodesCommand...");
                if (_LoadNodesCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing LoadNodesCommand...");
                    _LoadNodesCommand = new RelayCommand(LoadNodes);
                    MainViewModel.Instance.LogStatus("LoadNodesCommand initialized.");
                }
                return _LoadNodesCommand;
            }
        }

        private RelayCommand<NodeViewModel> _deleteNodesCommand;
        public RelayCommand<NodeViewModel> DeleteNodesCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing DeleteNodesCommand...");
                if (_deleteNodesCommand == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing DeleteNodesCommand...");
                    _deleteNodesCommand = new RelayCommand<NodeViewModel>(DeleteSelectedNodes);
                    MainViewModel.Instance.LogStatus("DeleteNodesCommand initialized.");
                }
                return _deleteNodesCommand;
            }
        }

        public DependencyObject MainWindow;
        public int BezierStrength = 80;

        private RelayCommand increaseBezier;
        public RelayCommand IncreaseBezierStrengthCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing IncreaseBezierStrengthCommand...");
                if (increaseBezier == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing IncreaseBezierStrengthCommand...");
                    increaseBezier = new RelayCommand(IncreaseBezier);
                    MainViewModel.Instance.LogStatus("IncreaseBezierStrengthCommand initialized.");
                }
                return increaseBezier;
            }
        }

        private RelayCommand decreaseBezier;
        public RelayCommand DecreaseBezierStrengthCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing DecreaseBezierStrengthCommand...");
                if (decreaseBezier == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing DecreaseBezierStrengthCommand...");
                    decreaseBezier = new RelayCommand(DecreaseBezier);
                    MainViewModel.Instance.LogStatus("DecreaseBezierStrengthCommand initialized.");
                }
                return decreaseBezier;
            }
        }

        private RelayCommand resetBezier;
        public RelayCommand ResetBezierStrengthCommand
        {
            get
            {
                MainViewModel.Instance.LogStatus("Accessing ResetBezierStrengthCommand...");
                if (resetBezier == null)
                {
                    MainViewModel.Instance.LogStatus("Initializing ResetBezierStrengthCommand...");
                    resetBezier = new RelayCommand(ResetBezier);
                    MainViewModel.Instance.LogStatus("ResetBezierStrengthCommand initialized.");
                }
                return resetBezier;
            }
        }


        public void IncreaseBezier()
        {
            try
            {
                MainViewModel.Instance.LogStatus($"Increasing BezierStrength. Current strength: {BezierStrength}");
                BezierStrength += 40;
                MainViewModel.Instance.LogStatus($"BezierStrength increased to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while increasing BezierStrength: {ex.Message}");
            }
        }

        public void DecreaseBezier()
        {
            try
            {
                MainViewModel.Instance.LogStatus($"Decreasing BezierStrength. Current strength: {BezierStrength}");
                BezierStrength -= 40;
                MainViewModel.Instance.LogStatus($"BezierStrength decreased to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while decreasing BezierStrength: {ex.Message}");
            }
        }

        public void ResetBezier()
        {
            try
            {
                MainViewModel.Instance.LogStatus("Resetting BezierStrength to default value (80).");
                BezierStrength = 80;
                MainViewModel.Instance.LogStatus($"BezierStrength reset to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while resetting BezierStrength: {ex.Message}");
            }
        }

        public void ConnectPinsFromSourceID(int sourceID, int destinationID)
        {
            try
            {
                MainViewModel.Instance.LogStatus($"Attempting to connect pins. Source ID: {sourceID}, Destination ID: {destinationID}");

                Connector source = GetConnectorWithID(sourceID);
                Connector destination = GetConnectorWithID(destinationID);

                if (source == null)
                {
                    MainViewModel.Instance.LogStatus($"Source connector with ID {sourceID} not found.");
                    return;
                }

                if (destination == null)
                {
                    MainViewModel.Instance.LogStatus($"Destination connector with ID {destinationID} not found.");
                    return;
                }

                MainViewModel.Instance.LogStatus($"Connecting source ID {sourceID} to destination ID {destinationID}.");
                Connector.ConnectPins(source, destination);
                MainViewModel.Instance.LogStatus($"Successfully connected source ID {sourceID} to destination ID {destinationID}.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while connecting pins from source ID {sourceID} to destination ID {destinationID}: {ex.Message}");
            }
        }

        public void SelectLocalizedString(LocalizationItem s)
        {
            try
            {
                MainViewModel.Instance.LogStatus($"Selecting localized string: {s.Key}");
                System.Windows.Controls.TextBox t = DynamicNode.GetCurrentEditTextBox();

                if (t != null)
                {
                    t.Text = s.Key;
                    MainViewModel.Instance.LogStatus($"TextBox updated with key: {s.Key}");
                }
                else
                {
                    MainViewModel.Instance.LogStatus("No active TextBox found. Cannot update with localized string.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while selecting localized string '{s.Key}': {ex.Message}");
            }
        }

        public string ExportNodesToLua()
        {
            MainViewModel.Instance.LogStatus("Starting Lua export of nodes.");
            StringBuilder luaScript = new StringBuilder();

            try
            {
                foreach (var node in this.Nodes)
                {
                    if (node is DynamicNode)
                    {
                        DynamicNode funcNode = (DynamicNode)node;
                        MainViewModel.Instance.LogStatus($"Exporting function: {funcNode.NodeName} to Lua.");

                        luaScript.AppendLine($"function {funcNode.NodeName}()");
                        luaScript.AppendLine(funcNode.NodeBody);
                        luaScript.AppendLine("end");

                        MainViewModel.Instance.LogStatus($"Function {funcNode.NodeName} exported.");
                    }
                    // Add additional cases for conditionals and loops
                }

                MainViewModel.Instance.LogStatus("Lua export completed.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error during Lua export: {ex.Message}");
            }

            return luaScript.ToString();
        }

        public string ExportNodesToPerl()
        {
            MainViewModel.Instance.LogStatus("Starting Perl export of nodes.");
            StringBuilder perlScript = new StringBuilder();

            try
            {
                foreach (var node in this.Nodes)
                {
                    if (node is DynamicNode)
                    {
                        DynamicNode funcNode = (DynamicNode)node;
                        MainViewModel.Instance.LogStatus($"Exporting function: {funcNode.NodeName} to Perl.");

                        perlScript.AppendLine($"sub {funcNode.NodeName} {{");
                        perlScript.AppendLine(funcNode.NodeBody);
                        perlScript.AppendLine("}");

                        MainViewModel.Instance.LogStatus($"Function {funcNode.NodeName} exported.");
                    }
                    // Add additional cases for conditionals and loops
                }

                MainViewModel.Instance.LogStatus("Perl export completed.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error during Perl export: {ex.Message}");
            }

            return perlScript.ToString();
        }



        // Modify the NetworkViewModel class to add nodes dynamically from parsed content.
        public void AddParsedNode(string nodeName, string nodeType, string body)
        {
            MainViewModel.Instance.LogStatus($"Attempting to add a parsed node. Name: {nodeName}, Type: {nodeType}");
            NodeViewModel nodeToAdd = null;

            try
            {
                // Create a new node for function, loop, or conditional
                switch (nodeType)
                {
                    case "Function":
                        MainViewModel.Instance.LogStatus($"Creating Function node: {nodeName}");
                        DynamicNode functionNode = new DynamicNode(mainViewModel);
                        functionNode.NodeName = nodeName;
                        functionNode.SetBody(body);
                        nodeToAdd = functionNode;
                        break;

                    case "If":
                    case "Conditional":
                        MainViewModel.Instance.LogStatus($"Creating Conditional node for: {nodeName}");
                        DynamicNode conditionalNode = new DynamicNode(mainViewModel);
                        conditionalNode.NodeName = "If statement";
                        nodeToAdd = conditionalNode;
                        break;

                    case "Loop":
                        MainViewModel.Instance.LogStatus($"Creating Loop node: {nodeName}");
                        DynamicNode loopNode = new DynamicNode(mainViewModel);
                        loopNode.NodeName = "Loop: " + nodeName;
                        nodeToAdd = loopNode;
                        break;

                    default:
                        MainViewModel.Instance.LogStatus($"Unknown node type: {nodeType}. Node not created.");
                        break;
                }

                if (nodeToAdd != null)
                {
                    MainViewModel.Instance.LogStatus($"Node created. Preparing to position and add to the grid.");

                    // Randomly position the new node on the grid
                    Point p = GetRandomPosition();
                    nodeToAdd.Margin = new Thickness(p.X, p.Y, 0, 0);
                    MainViewModel.Instance.LogStatus($"Node positioned at X: {p.X}, Y: {p.Y}");

                    // Add the node to the grid
                    Nodes.Add(nodeToAdd);
                    MainViewModel.Instance.LogStatus($"Node '{nodeToAdd.NodeName}' of type '{nodeType}' added to the grid.");
                }
                else
                {
                    MainViewModel.Instance.LogStatus($"Failed to add node: {nodeName}. Node was null.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while adding parsed node '{nodeName}': {ex.Message}");
            }
        }


        // Utility function to determine random node placement
        private Point GetRandomPosition()
        {
            MainViewModel.Instance.LogStatus("Calculating random position for new node...");
            try
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                if (main == null)
                {
                    MainViewModel.Instance.LogStatus("Main window reference is null. Cannot determine random position.");
                    return new Point(0, 0); // Return a default point if main window is null
                }

                Random r = new Random();
                int increment = r.Next(-400, 400);
                Point randomPoint = new Point(main.Width / 2 + increment, main.Height / 2 + increment);
                MainViewModel.Instance.LogStatus($"Random position calculated: X: {randomPoint.X}, Y: {randomPoint.Y}");
                return randomPoint;
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while calculating random position: {ex.Message}");
                return new Point(0, 0); // Return a default point in case of error
            }
        }


        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.LogStatus("AddNode_Click triggered.");
            try
            {
                // Fetch the NetworkViewModel from the DataContext
                NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();
                MainViewModel.Instance.LogStatus("Fetched NetworkViewModel instance.");

                // Create a new NodeWrapper (You can customize this based on your needs)
                NodeWrapper newNode = new NodeWrapper
                {
                    NodeName = "New Node",
                    TypeOfNode = NodeType.MethodNode,
                    Arguments = new List<Argument>(),
                    BaseAssemblyType = "System.String",
                    CallingClass = "MyClass"
                };
                MainViewModel.Instance.LogStatus($"Created new NodeWrapper with name: {newNode.NodeName}");

                // Use the existing AddNodeToGrid method to add the node to the grid
                network.AddNodeToGrid(newNode);
                MainViewModel.Instance.LogStatus($"Node '{newNode.NodeName}' added to the grid.");

                // Optionally, show a confirmation message
                MessageBox.Show("Node added to the grid!");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error in AddNode_Click: {ex.Message}");
            }
        }


        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.LogStatus("DeleteNode_Click triggered.");
            try
            {
                // Fetch the NetworkViewModel from the IoC container or DataContext
                NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();
                MainViewModel.Instance.LogStatus("Fetched NetworkViewModel instance.");

                // Find the node that is selected (where IsSelected == true)
                var selectedNode = network.Nodes.FirstOrDefault(node => node.IsSelected);
                MainViewModel.Instance.LogStatus(selectedNode != null
                    ? $"Selected node for deletion: {selectedNode.NodeName}"
                    : "No node selected for deletion.");

                // Check if a node is selected for deletion
                if (selectedNode != null)
                {
                    // Call the method to remove the selected node
                    network.RemoveNode(selectedNode);
                    MainViewModel.Instance.LogStatus($"Node '{selectedNode.NodeName}' removed from the grid.");

                    // Provide user feedback
                    MessageBox.Show($"Node '{selectedNode.NodeName}' deleted!");
                }
                else
                {
                    // No node selected for deletion
                    MessageBox.Show("No node selected to delete!");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error in DeleteNode_Click: {ex.Message}");
            }
        }





        public void AddNodeToGrid(NodeWrapper node)
        {
            // Use nodeFactory to create and add nodes
            NodeViewModel nodeToAdd = nodeFactory.CreateNode(node);

            if (nodeToAdd != null)
            {
                this.Nodes.Add(nodeToAdd);
                MainViewModel.Instance.LogStatus("Added node " + nodeToAdd.NodeName + " to grid");
            }
            else
            {
                MainViewModel.Instance.LogStatus("Couldn't add node " + node.NodeName + " to grid");
            }

            //Close the node view window
            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow.HideNodeListCommand.Execute(null);
        }

        public Connector GetConnectorWithID(int id)
        {
            MainViewModel.Instance.LogStatus($"Attempting to find connector with ID: {id}");
            Connector connector = null;

            try
            {
                // Check if MainWindow is null.
                if (MainWindow == null)
                {
                    MainViewModel.Instance.LogStatus("MainWindow is null. Cannot search for connectors.");
                    return null;
                }

                MainViewModel.Instance.LogStatus("Fetching all connectors from the MainWindow...");
                var connectors = FindVisualChildren<Connector>(MainWindow);

                MainViewModel.Instance.LogStatus($"Total connectors found: {connectors.Count()}");

                // Iterate through each connector and find the one matching the given ID.
                foreach (Connector cp in connectors)
                {
                    MainViewModel.Instance.LogStatus($"Checking connector with ID: {cp.ID}");
                    if (cp.ID == id)
                    {
                        MainViewModel.Instance.LogStatus($"Connector with ID: {id} found.");
                        return cp;
                    }
                }

                MainViewModel.Instance.LogStatus($"Connector with ID: {id} not found.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while finding connector with ID {id}: {ex.Message}");
            }

            return connector;
        }


        public void ClearNodes()
        {
            this.Nodes.Clear();
            MainViewModel.Instance.LogStatus("Cleared nodes.");
        }

        public void SaveNodes()
        {
            MainViewModel.Instance.LogStatus("Starting SaveNodes method...");

            try
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog
                {
                    // Set filter options and filter index.
                    Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                MainViewModel.Instance.LogStatus("Configured SaveFileDialog with filter options.");

                // Call the ShowDialog method to show the dialog box.
                bool? userClickedOK = saveFileDialog1.ShowDialog();
                MainViewModel.Instance.LogStatus($"Save dialog result: {userClickedOK}");

                // Process input if the user clicked OK.
                if (userClickedOK == true)
                {
                    string path = saveFileDialog1.FileName;
                    MainViewModel.Instance.LogStatus($"User selected path: {path}");

                    using (Stream myStream = saveFileDialog1.OpenFile())
                    {
                        if (myStream != null)
                        {
                            MainViewModel.Instance.LogStatus("Stream opened successfully. Preparing to save nodes...");

                            #region Nodes
                            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                            {
                                Indent = true,
                                IndentChars = "\t",
                                NewLineOnAttributes = true
                            };
                            MainViewModel.Instance.LogStatus("Configured XmlWriterSettings for serialization.");

                            List<SerializeableNodeViewModel> SerializeNodes = new List<SerializeableNodeViewModel>();
                            XmlSerializer serializer = new XmlSerializer(
                                typeof(List<SerializeableNodeViewModel>),
                                new Type[]
                                {
                            typeof(SerializeableVariableNode),
                            typeof(SerializeableConditionNode),
                            typeof(SerializeableRootNode),
                            typeof(SerializeableDynamicNode)
                                });
                            TextWriter writer = new StreamWriter(myStream);

                            MainViewModel.Instance.LogStatus("Serializing nodes...");

                            foreach (var node in Nodes)
                            {
                                if (node is RootNode rootNode)
                                {
                                    MainViewModel.Instance.LogStatus($"Processing RootNode: {rootNode.NodeName}");

                                    SerializeableRootNode rootSerial = new SerializeableRootNode
                                    {
                                        NodeName = rootNode.NodeName,
                                        NodeType = rootNode.NodeType,
                                        MarginX = rootNode.Margin.Left + rootNode.Transform.X,
                                        MarginY = rootNode.Margin.Top + rootNode.Transform.Y,
                                        ID = rootNode.ID,
                                        OutputNodeID = rootNode.OutputConnector.ConnectionNodeID,
                                        CallingClass = rootNode.CallingClass
                                    };
                                    SerializeNodes.Add(rootSerial);

                                    MainViewModel.Instance.LogStatus($"Serialized RootNode: {rootNode.NodeName}");
                                }
                                else if (node is ConditionNode conNode)
                                {
                                    MainViewModel.Instance.LogStatus($"Processing ConditionNode: {conNode.NodeName}");

                                    SerializeableConditionNode conSerial = new SerializeableConditionNode
                                    {
                                        NodeName = conNode.NodeName,
                                        NodeType = conNode.NodeType,
                                        MarginX = conNode.Margin.Left + conNode.Transform.X,
                                        MarginY = conNode.Margin.Top + conNode.Transform.Y,
                                        ID = conNode.ID,
                                        InputNodeID = conNode.InExecutionConnector.ConnectionNodeID,
                                        OutputTrueNodeID = conNode.OutExecutionConnectorTrue.ConnectionNodeID,
                                        OutputFalseNodeID = conNode.OutExecutionConnectorFalse.ConnectionNodeID,
                                        BoolVariableID = conNode.boolInput.ConnectionNodeID,
                                        BoolVariableName = conNode.ConnectedToVariableName,
                                        BoolCallingClass = conNode.ConnectedToVariableCallerClassName
                                    };
                                    SerializeNodes.Add(conSerial);

                                    MainViewModel.Instance.LogStatus($"Serialized ConditionNode: {conNode.NodeName}");
                                }
                                else if (node is VariableNode varNode)
                                {
                                    MainViewModel.Instance.LogStatus($"Processing VariableNode: {varNode.NodeName}");

                                    SerializeableVariableNode varSerial = new SerializeableVariableNode
                                    {
                                        NodeName = varNode.NodeName,
                                        TypeString = varNode.Type,
                                        NodeType = varNode.NodeType,
                                        MarginX = varNode.Margin.Left + varNode.Transform.X,
                                        MarginY = varNode.Margin.Top + varNode.Transform.Y,
                                        ID = varNode.ID,
                                        ConnectedToNodeID = varNode.NodeParameterOut.ConnectionNodeID,
                                        ConnectedToConnectorID = varNode.NodeParameterOut.ConnectedToConnectorID,
                                        CallingClass = varNode.CallingClass
                                    };
                                    SerializeNodes.Add(varSerial);

                                    MainViewModel.Instance.LogStatus($"Serialized VariableNode: {varNode.NodeName}");
                                }
                                else if (node is DynamicNode dynNode)
                                {
                                    MainViewModel.Instance.LogStatus($"Processing DynamicNode: {dynNode.NodeName}");

                                    SerializeableDynamicNode dynSerial = new SerializeableDynamicNode
                                    {
                                        NodeType = dynNode.NodeType,
                                        NodeName = dynNode.NodeName,
                                        Command = dynNode.Command,
                                        NodePanelHeight = dynNode.NodeHeight,
                                        MarginX = dynNode.Margin.Left + dynNode.Transform.X,
                                        MarginY = dynNode.Margin.Top + dynNode.Transform.Y,
                                        ID = dynNode.ID,
                                        InputNodeID = dynNode.InConnector.ConnectionNodeID,
                                        OutputNodeID = dynNode.OutConnector.ConnectionNodeID,
                                        CallingClass = dynNode.CallingClass
                                    };

                                    foreach (var arg in dynNode.ArgumentCache)
                                    {
                                        dynSerial.Arguments.Add(arg);
                                        MainViewModel.Instance.LogStatus($"Added argument: {arg.Name} to DynamicNode: {dynNode.NodeName}");
                                    }

                                    SerializeNodes.Add(dynSerial);
                                    MainViewModel.Instance.LogStatus($"Serialized DynamicNode: {dynNode.NodeName}");
                                }
                            }

                            serializer.Serialize(writer, SerializeNodes);
                            MainViewModel.Instance.LogStatus($"Nodes successfully serialized to path: {path}");

                            writer.Close();
                            MainViewModel.Instance.LogStatus("TextWriter closed after serialization.");

                            myStream.Close();
                            MainViewModel.Instance.LogStatus("Stream closed successfully.");

                            System.Windows.Clipboard.SetText(path);
                            MainViewModel.Instance.LogStatus($"Save completed. Path: {path} copied to clipboard", true);
                            #endregion
                        }
                        else
                        {
                            MainViewModel.Instance.LogStatus("Stream was null. Nodes were not saved.");
                        }
                    }
                }
                else
                {
                    MainViewModel.Instance.LogStatus("User canceled the save dialog.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error during SaveNodes: {ex.Message}");
            }
        }



        public void LoadNodes()
        {
            MainViewModel.Instance.LogStatus("Starting LoadNodes...");
            try
            {
                // Clear any existing nodes before loading new ones.
                MainViewModel.Instance.LogStatus("Clearing existing nodes...");
                ClearNodes();
                MainViewModel.Instance.LogStatus("Existing nodes cleared.");

                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                MainViewModel.Instance.LogStatus("Opening file dialog for node XML selection...");
                bool? userClickedOK = openFileDialog1.ShowDialog();
                MainViewModel.Instance.LogStatus($"File dialog result: {userClickedOK}");

                if (userClickedOK == true)
                {
                    string path = openFileDialog1.FileName;
                    MainViewModel.Instance.LogStatus($"Selected file path: {path}");

                    List<SerializeableNodeViewModel> SerializeNodes;
                    XmlSerializer ser = new XmlSerializer(typeof(List<SerializeableNodeViewModel>),
                        new Type[] { typeof(SerializeableVariableNode), typeof(SerializeableConditionNode), typeof(SerializeableDynamicNode), typeof(SerializeableRootNode) });

                    using (XmlReader reader = XmlReader.Create(path))
                    {
                        MainViewModel.Instance.LogStatus("Deserializing nodes from XML...");
                        SerializeNodes = (List<SerializeableNodeViewModel>)ser.Deserialize(reader);
                        MainViewModel.Instance.LogStatus($"Deserialization complete. {SerializeNodes.Count} nodes found.");
                    }

                    // Add the deserialized nodes to the collection.
                    foreach (var serializeableNodeViewModel in SerializeNodes)
                    {
                        try
                        {
                            if (serializeableNodeViewModel is SerializeableRootNode rootSerialized)
                            {
                                MainViewModel.Instance.LogStatus($"Processing SerializeableRootNode: {rootSerialized.NodeName}");
                                RootNode newNode = new RootNode(MainViewModel.Instance);
                                newNode.Populate(rootSerialized);
                                Nodes.Add(newNode);
                                MainViewModel.Instance.LogStatus($"Added RootNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableVariableNode variableSerialized)
                            {
                                MainViewModel.Instance.LogStatus($"Processing SerializeableVariableNode: {variableSerialized.NodeName}");
                                VariableNode newNode = new VariableNode(MainViewModel.Instance);
                                newNode.Populate(variableSerialized);
                                Nodes.Add(newNode);
                                MainViewModel.Instance.LogStatus($"Added VariableNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableDynamicNode dynamicSerialized)
                            {
                                MainViewModel.Instance.LogStatus($"Processing SerializeableDynamicNode: {dynamicSerialized.NodeName}");
                                DynamicNode newNode = new DynamicNode(MainViewModel.Instance);
                                newNode.Populate(dynamicSerialized);
                                Nodes.Add(newNode);
                                MainViewModel.Instance.LogStatus($"Added DynamicNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableConditionNode conSerialized)
                            {
                                MainViewModel.Instance.LogStatus($"Processing SerializeableConditionNode: {conSerialized.NodeName}");
                                ConditionNode newNode = new ConditionNode(MainViewModel.Instance);
                                newNode.Populate(conSerialized);
                                Nodes.Add(newNode);
                                MainViewModel.Instance.LogStatus($"Added ConditionNode: {newNode.NodeName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            MainViewModel.Instance.LogStatus($"Error while processing node: {ex.Message}");
                        }
                    }

                    // Connect nodes as per the saved configuration.
                    MainViewModel.Instance.LogStatus("Connecting nodes...");
                    ConnectNodes();
                    MainViewModel.Instance.LogStatus("Nodes connected successfully.");
                }
                else
                {
                    MainViewModel.Instance.LogStatus("User canceled the file dialog. No nodes loaded.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error in LoadNodes: {ex.Message}");
            }
            MainViewModel.Instance.LogStatus("LoadNodes process completed.");
        }

        private void ConnectNodes()
        {
            try
            {
                foreach (var node in Nodes)
                {
                    if (node is RootNode rootNode)
                    {
                        MainViewModel.Instance.LogStatus($"Connecting output for RootNode: {rootNode.NodeName}");
                        if (rootNode.OutputConnector.ConnectionNodeID <= 0)
                        {
                            MainViewModel.Instance.LogStatus("Output connection ID is not valid, skipping connection.");
                            continue;
                        }

                        Connector connectedTo = GetInConnectorBasedOnNode(rootNode.OutputConnector.ConnectionNodeID);
                        Connector.ConnectPins(rootNode.OutputConnector, connectedTo);
                        MainViewModel.Instance.LogStatus($"Output connected for RootNode: {rootNode.NodeName}");
                    }

                    if (node is ConditionNode conNode)
                    {
                        MainViewModel.Instance.LogStatus($"Connecting ConditionNode: {conNode.NodeName}");

                        if (conNode.boolInput.ConnectionNodeID > 0)
                        {
                            Connector connectedToVar = GetOutConnectorBasedOnNode(conNode.boolInput.ConnectionNodeID);
                            Connector.ConnectPins(conNode.boolInput, connectedToVar);
                            MainViewModel.Instance.LogStatus($"Bool input connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.InExecutionConnector.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetOutConnectorBasedOnNode(conNode.InExecutionConnector.ConnectionNodeID);
                            Connector.ConnectPins(conNode.InExecutionConnector, connectedTo);
                            MainViewModel.Instance.LogStatus($"Input connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.OutExecutionConnectorTrue.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorTrue.ConnectionNodeID);
                            Connector.ConnectPins(conNode.OutExecutionConnectorTrue, connectedTo);
                            MainViewModel.Instance.LogStatus($"Output (True) connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.OutExecutionConnectorFalse.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorFalse.ConnectionNodeID);
                            Connector.ConnectPins(conNode.OutExecutionConnectorFalse, connectedTo);
                            MainViewModel.Instance.LogStatus($"Output (False) connected for ConditionNode: {conNode.NodeName}");
                        }
                    }

                    if (node is DynamicNode dynNode)
                    {
                        MainViewModel.Instance.LogStatus($"Connecting DynamicNode: {dynNode.NodeName}");

                        for (int i = 0; i < dynNode.ArgumentCache.Count(); i++)
                        {
                            Argument arg = dynNode.ArgumentCache.ElementAt(i);

                            if (arg.ArgIsExistingVariable)
                            {
                                Connector conID = dynNode.GetConnectorAtIndex(i);
                                int connectedToVar = arg.ArgumentConnectedToNodeID;
                                Connector varConnect = GetOutConnectorBasedOnNode(connectedToVar);
                                Connector.ConnectPins(conID, varConnect);
                                MainViewModel.Instance.LogStatus($"Argument connected for DynamicNode: {dynNode.NodeName}, Argument: {arg.Name}");
                            }
                        }

                        if (dynNode.OutExecutionConnector.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(dynNode.OutExecutionConnector.ConnectionNodeID);
                            Connector.ConnectPins(dynNode.OutExecutionConnector, connectedTo);
                            MainViewModel.Instance.LogStatus($"Output connected for DynamicNode: {dynNode.NodeName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while connecting nodes: {ex.Message}");
            }
        }



        // Method to parse Lua script and create nodes dynamically
        public void ImportLuaFile(string filePath)
        {
            MainViewModel.Instance.LogStatus($"Starting ImportLuaFile with filePath: {filePath}");
            try
            {
                // Read the Lua script from the specified file path
                string luaScript = File.ReadAllText(filePath);
                MainViewModel.Instance.LogStatus("Lua script read successfully.");

                var lua = new Script();
                MainViewModel.Instance.LogStatus("Lua script environment initialized.");

                // Load the script into the Lua environment (this will not execute the script)
                lua.LoadString(luaScript);
                MainViewModel.Instance.LogStatus("Lua script loaded into environment.");

                // Parse functions
                MainViewModel.Instance.LogStatus("Parsing functions from Lua script...");
                foreach (var global in lua.Globals.Keys)
                {
                    DynValue globalValue = lua.Globals.Get(global);
                    if (globalValue.Type == DataType.Function)
                    {
                        string functionName = global.ToString();
                        string functionBody = GetLuaFunctionBody(luaScript, functionName);
                        MainViewModel.Instance.LogStatus($"Found function: {functionName}, adding as node.");
                        AddParsedNode(functionName, "Function", functionBody);
                        MainViewModel.Instance.LogStatus($"Function '{functionName}' added as node.");
                    }
                }
                MainViewModel.Instance.LogStatus("Function parsing completed.");

                // Parse conditionals
                MainViewModel.Instance.LogStatus("Parsing conditionals...");
                ParseConditionals(luaScript);
                MainViewModel.Instance.LogStatus("Conditionals parsing completed.");

                // Parse loops
                MainViewModel.Instance.LogStatus("Parsing loops...");
                ParseLoops(luaScript);
                MainViewModel.Instance.LogStatus("Loop parsing completed.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error in ImportLuaFile: {ex.Message}");
            }
        }

        // Method to parse conditionals like "if" and "else"
        private void ParseConditionals(string luaScript)
        {
            MainViewModel.Instance.LogStatus("Starting ParseConditionals...");
            try
            {
                string ifPattern = @"if\s+\(.*?\)\s+then(.*?)end";
                var matches = Regex.Matches(luaScript, ifPattern, RegexOptions.Singleline);
                MainViewModel.Instance.LogStatus($"Found {matches.Count} 'if' conditionals in the Lua script.");

                foreach (Match match in matches)
                {
                    string conditionBody = match.Groups[1].Value;
                    MainViewModel.Instance.LogStatus("Adding 'If' conditional as node...");
                    AddParsedNode("Conditional", "If", conditionBody);
                    MainViewModel.Instance.LogStatus("'If' conditional node added.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while parsing conditionals: {ex.Message}");
            }
            MainViewModel.Instance.LogStatus("ParseConditionals completed.");
        }

        // Method to parse loops like "for" and "while"
        private void ParseLoops(string luaScript)
        {
            MainViewModel.Instance.LogStatus("Starting ParseLoops...");
            try
            {
                // For-loop pattern
                string forPattern = @"for\s+.*?\s+in\s+.*?\s+do(.*?)end";
                var forMatches = Regex.Matches(luaScript, forPattern, RegexOptions.Singleline);
                MainViewModel.Instance.LogStatus($"Found {forMatches.Count} 'for' loops in the Lua script.");

                foreach (Match match in forMatches)
                {
                    string loopBody = match.Groups[1].Value;
                    MainViewModel.Instance.LogStatus("Adding 'For' loop as node...");
                    AddParsedNode("Loop", "For", loopBody);
                    MainViewModel.Instance.LogStatus("'For' loop node added.");
                }

                // While-loop pattern
                string whilePattern = @"while\s+.*?\s+do(.*?)end";
                var whileMatches = Regex.Matches(luaScript, whilePattern, RegexOptions.Singleline);
                MainViewModel.Instance.LogStatus($"Found {whileMatches.Count} 'while' loops in the Lua script.");

                foreach (Match match in whileMatches)
                {
                    string loopBody = match.Groups[1].Value;
                    MainViewModel.Instance.LogStatus("Adding 'While' loop as node...");
                    AddParsedNode("Loop", "While", loopBody);
                    MainViewModel.Instance.LogStatus("'While' loop node added.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while parsing loops: {ex.Message}");
            }
            MainViewModel.Instance.LogStatus("ParseLoops completed.");
        }

        // Helper function to extract function body from Lua script by name
        private string GetLuaFunctionBody(string luaScript, string functionName)
        {
            MainViewModel.Instance.LogStatus($"Extracting body for function: {functionName}");
            try
            {
                // Simple method to find and extract the function body for demonstration purposes
                string pattern = $@"function\s+{functionName}\s*\(.*?\)(.*?)end";
                var match = Regex.Match(luaScript, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    MainViewModel.Instance.LogStatus($"Body extracted for function: {functionName}");
                    return match.Groups[1].Value;
                }
                else
                {
                    MainViewModel.Instance.LogStatus($"No body found for function: {functionName}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error extracting function body for {functionName}: {ex.Message}");
                return string.Empty;
            }
        }


        public void ImportPerlFile(string filePath)
        {
            MainViewModel.Instance.LogStatus($"Starting ImportPerlFile with filePath: {filePath}");
            try
            {
                // Define Perl script path and arguments
                string perlExecutable = @"C:\Strawberry\perl\bin\perl.exe";  // Path to your Perl executable
                string scriptPath = @"C:\Users\corys\Source\Repos\OR10N\FlowParser\parse_perl_script.pl";  // A Perl script that uses PPI to parse another Perl file

                MainViewModel.Instance.LogStatus($"Perl executable: {perlExecutable}");
                MainViewModel.Instance.LogStatus($"Script path: {scriptPath}");

                // Create a new process to execute the Perl script
                Process perlProcess = new Process();
                perlProcess.StartInfo.FileName = perlExecutable;
                perlProcess.StartInfo.Arguments = $"\"{scriptPath}\" \"{filePath}\"";  // Pass the file path as an argument
                perlProcess.StartInfo.RedirectStandardOutput = true;
                perlProcess.StartInfo.UseShellExecute = false;
                perlProcess.StartInfo.CreateNoWindow = true;

                MainViewModel.Instance.LogStatus("Starting Perl process...");

                // Start the Perl script and capture the output
                perlProcess.Start();
                string output = perlProcess.StandardOutput.ReadToEnd();
                perlProcess.WaitForExit();
                MainViewModel.Instance.LogStatus("Perl process completed. Output captured.");

                // Process the output in C#
                ParsePerlSubroutines(output);
                MainViewModel.Instance.LogStatus("Perl subroutine parsing completed.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error in ImportPerlFile: {ex.Message}");
            }
        }

        // Method to parse the Perl script output (example)
        private void ParsePerlSubroutines(string output)
        {
            MainViewModel.Instance.LogStatus("Parsing Perl subroutines from output...");
            try
            {
                // Here, you will split and parse the output from the Perl script
                // Assuming the output format is a list of subroutine names and bodies
                var subroutines = output.Split(new string[] { "Subroutine:" }, StringSplitOptions.RemoveEmptyEntries);
                MainViewModel.Instance.LogStatus($"Found {subroutines.Length} subroutines.");

                foreach (var sub in subroutines)
                {
                    var lines = sub.Split('\n');
                    var name = lines[0].Trim();  // Subroutine name
                    var body = string.Join("\n", lines.Skip(1));  // Subroutine body

                    MainViewModel.Instance.LogStatus($"Parsing subroutine: {name}");

                    // Create a new node in the flowchart for each subroutine
                    AddParsedNode(name, "Subroutine", body);
                    MainViewModel.Instance.LogStatus($"Subroutine '{name}' added as a node.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while parsing Perl subroutines: {ex.Message}");
            }
        }

        // Finds children of a specific type in a visual tree
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            MainViewModel.Instance.LogStatus($"Finding visual children of type: {typeof(T).Name}");
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    MainViewModel.Instance.LogStatus($"Inspecting child at index {i}: {child?.GetType().Name ?? "null"}");

                    if (child != null && child is T)
                    {
                        MainViewModel.Instance.LogStatus($"Found child of type {typeof(T).Name}");
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // Property for node collection
        private ObservableCollection<NodeViewModel> nodes = null;
        public ObservableCollection<NodeViewModel> Nodes { get; set; }

        // Property for selected nodes
        public static ObservableCollection<NodeViewModel> SelectedNodes { get; set; }

        // Method to delete selected nodes with logging
        public void DeleteSelectedNodes(NodeViewModel node)
        {
            MainViewModel.Instance.LogStatus($"Attempting to delete selected node: {node?.NodeName ?? "Unknown"}");
            try
            {
                if (node != null)
                {
                    MainViewModel.Instance.LogStatus($"Disconnecting all connectors for node: {node.NodeName}");
                    node.DisconnectAllConnectors();
                    Nodes.Remove(node);
                    MainViewModel.Instance.LogStatus($"Node '{node.NodeName}' removed from the collection.");
                }
                else
                {
                    MainViewModel.Instance.LogStatus("No node selected for deletion.");
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while deleting node: {ex.Message}");
            }
        }


        public Connector GetInConnectorBasedOnNode(int nodeID)
        {
            MainViewModel.Instance.LogStatus($"Attempting to find InConnector for node with ID: {nodeID}");

            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
            {
                MainViewModel.Instance.LogStatus($"No node found with ID: {nodeID}. Returning null for InConnector.");
                return null;
            }

            try
            {
                if (node is DynamicNode dyn)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is a DynamicNode. Returning InConnector.");
                    return dyn.InConnector;
                }

                if (node is ConditionNode con)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is a ConditionNode. Returning InExecutionConnector.");
                    return con.InExecutionConnector;
                }

                MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is not a recognized type for InConnector. Returning null.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while retrieving InConnector for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }

        public Connector GetOutConnectorBasedOnNode(int nodeID)
        {
            MainViewModel.Instance.LogStatus($"Attempting to find OutConnector for node with ID: {nodeID}");

            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
            {
                MainViewModel.Instance.LogStatus($"No node found with ID: {nodeID}. Returning null for OutConnector.");
                return null;
            }

            try
            {
                if (node is RootNode root)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is a RootNode. Returning OutputConnector.");
                    return root.OutputConnector;
                }

                if (node is DynamicNode dyn)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is a DynamicNode. Returning OutConnector.");
                    return dyn.OutConnector;
                }

                if (node is VariableNode varNode)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is a VariableNode. Returning NodeParameterOut.");
                    return varNode.NodeParameterOut;
                }

                MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} is not a recognized type for OutConnector. Returning null.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while retrieving OutConnector for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }

        public NodeViewModel GetNodeByID(int nodeID)
        {
            MainViewModel.Instance.LogStatus($"Searching for node with ID: {nodeID}.");

            try
            {
                var node = from n in Nodes
                           where n.ID == nodeID
                           select n;

                if (node.Count() == 1)
                {
                    MainViewModel.Instance.LogStatus($"Node with ID: {nodeID} found. Returning the node.");
                    return node.First();
                }

                MainViewModel.Instance.LogStatus($"No unique node found with ID: {nodeID}. Found {node.Count()} matches. Returning null.");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogStatus($"Error while searching for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }


        public NetworkViewModel(MainViewModel mainViewModel)
{
    MainViewModel.Instance.LogStatus("Initializing NetworkViewModel...");

    try
    {
        AddedNodesOrder = new List<NodeViewModel>();
        RemovedNodesOrder = new List<NodeViewModel>();
        Nodes = new ObservableCollection<NodeViewModel>();
        MainViewModel.Instance.LogStatus("Node lists initialized.");

        // Initialize Undo/Redo commands
        MainViewModel.Instance.LogStatus("Setting up Undo/Redo commands...");
        UndoCommand = new RelayCommand(Undo, CanUndo);
        RedoCommand = new RelayCommand(Redo, CanRedo);
        MainViewModel.Instance.LogStatus("Undo/Redo commands initialized.");

        // Correctly instantiate NodeFactory with the MainViewModel singleton instance
        nodeFactory = new NodeFactory(MainViewModel.Instance);
        this.mainViewModel = mainViewModel;
        MainViewModel.Instance.LogStatus("NodeFactory initialized and MainViewModel instance assigned.");

        MainViewModel.Instance.LogStatus("NetworkViewModel initialized successfully.");
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error during NetworkViewModel initialization: {ex.Message}");
    }
}

// Add a new undoable action to the stack
public void AddUndoableAction(UndoableAction action)
{
    MainViewModel.Instance.LogStatus("Adding a new undoable action...");
    try
    {
        undoStack.Push(action);
        redoStack.Clear();  // Clear redo stack when new action is added
        RefreshCommandStates();
        MainViewModel.Instance.LogStatus("Undoable action added successfully and redo stack cleared.");
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error while adding undoable action: {ex.Message}");
    }
}

// Perform Undo
private void Undo()
{
    MainViewModel.Instance.LogStatus("Attempting to perform Undo...");
    try
    {
        if (undoStack.Any())
        {
            var action = undoStack.Pop();
            MainViewModel.Instance.LogStatus("Undo action found, executing Undo...");
            action.Undo();
            redoStack.Push(action);
            MainViewModel.Instance.LogStatus("Undo executed successfully and action pushed to redo stack.");
            RefreshCommandStates();
        }
        else
        {
            MainViewModel.Instance.LogStatus("No actions available to undo.");
        }
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error during Undo operation: {ex.Message}");
    }
}

// Perform Redo
private void Redo()
{
    MainViewModel.Instance.LogStatus("Attempting to perform Redo...");
    try
    {
        if (redoStack.Any())
        {
            var action = redoStack.Pop();
            MainViewModel.Instance.LogStatus("Redo action found, executing Redo...");
            action.Execute();
            undoStack.Push(action);
            MainViewModel.Instance.LogStatus("Redo executed successfully and action pushed to undo stack.");
            RefreshCommandStates();
        }
        else
        {
            MainViewModel.Instance.LogStatus("No actions available to redo.");
        }
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error during Redo operation: {ex.Message}");
    }
}

// Enable/Disable Undo button
private bool CanUndo()
{
    bool canUndo = undoStack.Any();
    MainViewModel.Instance.LogStatus($"CanUndo check: {canUndo}");
    return canUndo;
}

// Enable/Disable Redo button
private bool CanRedo()
{
    bool canRedo = redoStack.Any();
    MainViewModel.Instance.LogStatus($"CanRedo check: {canRedo}");
    return canRedo;
}

private void RefreshCommandStates()
{
    MainViewModel.Instance.LogStatus("Refreshing command states for Undo/Redo...");
    try
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        MainViewModel.Instance.LogStatus("Command states refreshed successfully.");
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error while refreshing command states: {ex.Message}");
    }
}

public void RemoveNode(NodeViewModel node)
{
    MainViewModel.Instance.LogStatus($"Attempting to remove node: {node?.NodeName ?? "Unknown"}");
    try
    {
        if (node != null && Nodes.Contains(node))
        {
            Nodes.Remove(node);
            MainViewModel.Instance.LogStatus($"Node {node.NodeName} removed from the grid.");
        }
        else
        {
            MainViewModel.Instance.LogStatus("Failed to remove node - node not found in the collection.");
        }
    }
    catch (Exception ex)
    {
        MainViewModel.Instance.LogStatus($"Error while removing node: {ex.Message}");
    }
}


        }
    }




