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
using System.Runtime.CompilerServices;
using FlowParser;

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
                Log.Info("Creating UndoableAction.");
                DoAction = doAction;
                UndoAction = undoAction;
                Log.Info("UndoableAction created.");
            }

            public void Execute()
            {
                Log.Info("Executing UndoableAction.");
                DoAction();
            }

            public void Undo()
            {
                Log.Info("Undoing action in UndoableAction.");
                UndoAction();
            }
        }

        private RelayCommand<NodeWrapper> _addNodeToGridCommand;
        public RelayCommand<NodeWrapper> AddNodeToGridCommand
        {
            get
            {
                Log.Info("Accessing AddNodeToGridCommand...");
                if (_addNodeToGridCommand == null)
                {
                    Log.Info("Initializing AddNodeToGridCommand...");
                    _addNodeToGridCommand = new RelayCommand<NodeWrapper>(AddNodeToGrid);
                    Log.Info("AddNodeToGridCommand initialized.");
                }
                return _addNodeToGridCommand;
            }
        }

        private RelayCommand<LocalizationItem> _selectLocalizedStringCommand;
        public RelayCommand<LocalizationItem> SelectLocalizedStringCommand
        {
            get
            {
                Log.Info("Accessing SelectLocalizedStringCommand...");
                if (_selectLocalizedStringCommand == null)
                {
                    Log.Info("Initializing SelectLocalizedStringCommand...");
                    _selectLocalizedStringCommand = new RelayCommand<LocalizationItem>(SelectLocalizedString);
                    Log.Info("SelectLocalizedStringCommand initialized.");
                }
                return _selectLocalizedStringCommand;
            }
        }

        private RelayCommand _SaveNodesCommand;
        public RelayCommand SaveNodesCommand
        {
            get
            {
                Log.Info("Accessing SaveNodesCommand...");
                if (_SaveNodesCommand == null)
                {
                    Log.Info("Initializing SaveNodesCommand...");
                    _SaveNodesCommand = new RelayCommand(SaveNodes);
                    Log.Info("SaveNodesCommand initialized.");
                }
                return _SaveNodesCommand;
            }
        }

        private RelayCommand _ClearNodesCommand;
        public RelayCommand ClearNodesCommand
        {
            get
            {
                Log.Info("Accessing ClearNodesCommand...");
                if (_ClearNodesCommand == null)
                {
                    Log.Info("Initializing ClearNodesCommand...");
                    _ClearNodesCommand = new RelayCommand(ClearNodes);
                    Log.Info("ClearNodesCommand initialized.");
                }
                return _ClearNodesCommand;
            }
        }

        private RelayCommand _LoadNodesCommand;
        public RelayCommand LoadNodesCommand
        {
            get
            {
                Log.Info("Accessing LoadNodesCommand...");
                if (_LoadNodesCommand == null)
                {
                    Log.Info("Initializing LoadNodesCommand...");
                    _LoadNodesCommand = new RelayCommand(LoadNodes);
                    Log.Info("LoadNodesCommand initialized.");
                }
                return _LoadNodesCommand;
            }
        }

        private RelayCommand<NodeViewModel> _deleteNodesCommand;
        public RelayCommand<NodeViewModel> DeleteNodesCommand
        {
            get
            {
                Log.Info("Accessing DeleteNodesCommand...");
                if (_deleteNodesCommand == null)
                {
                    Log.Info("Initializing DeleteNodesCommand...");
                    _deleteNodesCommand = new RelayCommand<NodeViewModel>(DeleteSelectedNodes);
                    Log.Info("DeleteNodesCommand initialized.");
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
                Log.Info("Accessing IncreaseBezierStrengthCommand...");
                if (increaseBezier == null)
                {
                    Log.Info("Initializing IncreaseBezierStrengthCommand...");
                    increaseBezier = new RelayCommand(IncreaseBezier);
                    Log.Info("IncreaseBezierStrengthCommand initialized.");
                }
                return increaseBezier;
            }
        }

        private RelayCommand decreaseBezier;
        public RelayCommand DecreaseBezierStrengthCommand
        {
            get
            {
                Log.Info("Accessing DecreaseBezierStrengthCommand...");
                if (decreaseBezier == null)
                {
                    Log.Info("Initializing DecreaseBezierStrengthCommand...");
                    decreaseBezier = new RelayCommand(DecreaseBezier);
                    Log.Info("DecreaseBezierStrengthCommand initialized.");
                }
                return decreaseBezier;
            }
        }

        private RelayCommand resetBezier;
        public RelayCommand ResetBezierStrengthCommand
        {
            get
            {
                Log.Info("Accessing ResetBezierStrengthCommand...");
                if (resetBezier == null)
                {
                    Log.Info("Initializing ResetBezierStrengthCommand...");
                    resetBezier = new RelayCommand(ResetBezier);
                    Log.Info("ResetBezierStrengthCommand initialized.");
                }
                return resetBezier;
            }
        }


        public void IncreaseBezier()
        {
            try
            {
                Log.Info($"Increasing BezierStrength. Current strength: {BezierStrength}");
                BezierStrength += 40;
                Log.Info($"BezierStrength increased to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while increasing BezierStrength: {ex.Message}");
            }
        }

        public void DecreaseBezier()
        {
            try
            {
                Log.Info($"Decreasing BezierStrength. Current strength: {BezierStrength}");
                BezierStrength -= 40;
                Log.Info($"BezierStrength decreased to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while decreasing BezierStrength: {ex.Message}");
            }
        }

        public void ResetBezier()
        {
            try
            {
                Log.Info("Resetting BezierStrength to default value (80).");
                BezierStrength = 80;
                Log.Info($"BezierStrength reset to: {BezierStrength}");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while resetting BezierStrength: {ex.Message}");
            }
        }

        public void ConnectPinsFromSourceID(int sourceID, int destinationID)
        {
            try
            {
                Log.Info($"Attempting to connect pins. Source ID: {sourceID}, Destination ID: {destinationID}");

                Connector source = GetConnectorWithID(sourceID);
                Connector destination = GetConnectorWithID(destinationID);

                if (source == null)
                {
                    Log.Info($"Source connector with ID {sourceID} not found.");
                    return;
                }

                if (destination == null)
                {
                    Log.Info($"Destination connector with ID {destinationID} not found.");
                    return;
                }

                Log.Info($"Connecting source ID {sourceID} to destination ID {destinationID}.");
                Connector.ConnectPins(source, destination);
                Log.Info($"Successfully connected source ID {sourceID} to destination ID {destinationID}.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while connecting pins from source ID {sourceID} to destination ID {destinationID}: {ex.Message}");
            }
        }
        

        public void SelectLocalizedString(LocalizationItem s)
        {
            try
            {
                Log.Info($"Selecting localized string: {s.Key}");
                System.Windows.Controls.TextBox t = DynamicNode.GetCurrentEditTextBox();

                if (t != null)
                {
                    t.Text = s.Key;
                    Log.Info($"TextBox updated with key: {s.Key}");
                }
                else
                {
                    Log.Info("No active TextBox found. Cannot update with localized string.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while selecting localized string '{s.Key}': {ex.Message}");
            }
        }

        public string ExportNodesToLua()
        {
            Log.Info("Starting Lua export of nodes.");
            StringBuilder luaScript = new StringBuilder();

            try
            {
                foreach (var node in this.Nodes)
                {
                    if (node is DynamicNode)
                    {
                        DynamicNode funcNode = (DynamicNode)node;
                        Log.Info($"Exporting function: {funcNode.NodeName} to Lua.");

                        luaScript.AppendLine($"function {funcNode.NodeName}()");
                        luaScript.AppendLine(funcNode.NodeBody);
                        luaScript.AppendLine("end");

                        Log.Info($"Function {funcNode.NodeName} exported.");
                    }
                    // Add additional cases for conditionals and loops
                }

                Log.Info("Lua export completed.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error during Lua export: {ex.Message}");
            }

            return luaScript.ToString();
        }

        public string ExportNodesToPerl()
        {
            Log.Info("Starting Perl export of nodes.");
            StringBuilder perlScript = new StringBuilder();

            try
            {
                foreach (var node in this.Nodes)
                {
                    if (node is DynamicNode)
                    {
                        DynamicNode funcNode = (DynamicNode)node;
                        Log.Info($"Exporting function: {funcNode.NodeName} to Perl.");

                        perlScript.AppendLine($"sub {funcNode.NodeName} {{");
                        perlScript.AppendLine(funcNode.NodeBody);
                        perlScript.AppendLine("}");

                        Log.Info($"Function {funcNode.NodeName} exported.");
                    }
                    // Add additional cases for conditionals and loops
                }

                Log.Info("Perl export completed.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error during Perl export: {ex.Message}");
            }

            return perlScript.ToString();
        }



        // Modify the NetworkViewModel class to add nodes dynamically from parsed content.
        public void AddParsedNode(string nodeName, string nodeType, string body)
        {
            Log.Info($"Attempting to add a parsed node. Name: {nodeName}, Type: {nodeType}");
            NodeViewModel nodeToAdd = null;

            try
            {
                // Create a new node for function, loop, or conditional
                switch (nodeType)
                {
                    case "Function":
                        Log.Info($"Creating Function node: {nodeName}");
                        DynamicNode functionNode = new DynamicNode(mainViewModel);
                        functionNode.NodeName = nodeName;
                        functionNode.SetBody(body);
                        nodeToAdd = functionNode;
                        break;

                    case "If":
                    case "Conditional":
                        Log.Info($"Creating Conditional node for: {nodeName}");
                        DynamicNode conditionalNode = new DynamicNode(mainViewModel);
                        conditionalNode.NodeName = "If statement";
                        nodeToAdd = conditionalNode;
                        break;

                    case "Loop":
                        Log.Info($"Creating Loop node: {nodeName}");
                        DynamicNode loopNode = new DynamicNode(mainViewModel);
                        loopNode.NodeName = "Loop: " + nodeName;
                        nodeToAdd = loopNode;
                        break;

                    default:
                        Log.Info($"Unknown node type: {nodeType}. Node not created.");
                        break;
                }

                if (nodeToAdd != null)
                {
                    Log.Info($"Node created. Preparing to position and add to the grid.");

                    // Randomly position the new node on the grid
                    Point p = GetRandomPosition();
                    nodeToAdd.Margin = new Thickness(p.X, p.Y, 0, 0);
                    Log.Info($"Node positioned at X: {p.X}, Y: {p.Y}");

                    // Add the node to the grid
                    Nodes.Add(nodeToAdd);
                    Log.Info($"Node '{nodeToAdd.NodeName}' of type '{nodeType}' added to the grid.");
                }
                else
                {
                    Log.Info($"Failed to add node: {nodeName}. Node was null.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while adding parsed node '{nodeName}': {ex.Message}");
            }
        }


        // Utility function to determine random node placement
        private Point GetRandomPosition()
        {
            Log.Info("Calculating random position for new node...");
            try
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                if (main == null)
                {
                    Log.Info("Main window reference is null. Cannot determine random position.");
                    return new Point(0, 0); // Return a default point if main window is null
                }

                Random r = new Random();
                int increment = r.Next(-400, 400);
                Point randomPoint = new Point(main.Width / 2 + increment, main.Height / 2 + increment);
                Log.Info($"Random position calculated: X: {randomPoint.X}, Y: {randomPoint.Y}");
                return randomPoint;
            }
            catch (Exception ex)
            {
                Log.Info($"Error while calculating random position: {ex.Message}");
                return new Point(0, 0); // Return a default point in case of error
            }
        }


        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("AddNode_Click triggered.");
            try
            {
                // Fetch the NetworkViewModel from the DataContext
                NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();
                Log.Info("Fetched NetworkViewModel instance.");

                // Create a new NodeWrapper (You can customize this based on your needs)
                NodeWrapper newNode = new NodeWrapper
                {
                    NodeName = "New Node",
                    TypeOfNode = NodeType.MethodNode,
                    Arguments = new List<Argument>(),
                    BaseAssemblyType = "System.String",
                    CallingClass = "MyClass"
                };
                Log.Info($"Created new NodeWrapper with name: {newNode.NodeName}");

                // Use the existing AddNodeToGrid method to add the node to the grid
                network.AddNodeToGrid(newNode);
                Log.Info($"Node '{newNode.NodeName}' added to the grid.");

                // Optionally, show a confirmation message
                MessageBox.Show("Node added to the grid!");
            }
            catch (Exception ex)
            {
                Log.Info($"Error in AddNode_Click: {ex.Message}");
            }
        }


        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("DeleteNode_Click triggered.");
            try
            {
                // Fetch the NetworkViewModel from the IoC container or DataContext
                NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();
                Log.Info("Fetched NetworkViewModel instance.");

                // Find the node that is selected (where IsSelected == true)
                var selectedNode = network.Nodes.FirstOrDefault(node => node.IsSelected);
                Log.Info(selectedNode != null
                    ? $"Selected node for deletion: {selectedNode.NodeName}"
                    : "No node selected for deletion.");

                // Check if a node is selected for deletion
                if (selectedNode != null)
                {
                    // Call the method to remove the selected node
                    network.RemoveNode(selectedNode);
                    Log.Info($"Node '{selectedNode.NodeName}' removed from the grid.");

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
                Log.Info($"Error in DeleteNode_Click: {ex.Message}");
            }
        }





        public void AddNodeToGrid(NodeWrapper node)
        {
            // Use nodeFactory to create and add nodes
            NodeViewModel nodeToAdd = nodeFactory.CreateNode(node);

            if (nodeToAdd != null)
            {
                this.Nodes.Add(nodeToAdd);
                Log.Info("Added node " + nodeToAdd.NodeName + " to grid");
            }
            else
            {
                Log.Info("Couldn't add node " + node.NodeName + " to grid");
            }

            //Close the node view window
            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow.HideNodeListCommand.Execute(null);
        }

        public Connector GetConnectorWithID(int id)
        {
            Log.Info($"Attempting to find connector with ID: {id}");
            Connector connector = null;

            try
            {
                // Check if MainWindow is null.
                if (MainWindow == null)
                {
                    Log.Info("MainWindow is null. Cannot search for connectors.");
                    return null;
                }

                Log.Info("Fetching all connectors from the MainWindow...");
                var connectors = FindVisualChildren<Connector>(MainWindow);

                Log.Info($"Total connectors found: {connectors.Count()}");

                // Iterate through each connector and find the one matching the given ID.
                foreach (Connector cp in connectors)
                {
                    Log.Info($"Checking connector with ID: {cp.ID}");
                    if (cp.ID == id)
                    {
                        Log.Info($"Connector with ID: {id} found.");
                        return cp;
                    }
                }

                Log.Info($"Connector with ID: {id} not found.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while finding connector with ID {id}: {ex.Message}");
            }

            return connector;
        }


        public void ClearNodes()
        {
            this.Nodes.Clear();
            Log.Info("Cleared nodes.");
        }

        public void SaveNodes()
        {
            Log.Info("Starting SaveNodes method...");

            try
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog
                {
                    // Set filter options and filter index.
                    Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                Log.Info("Configured SaveFileDialog with filter options.");

                // Call the ShowDialog method to show the dialog box.
                bool? userClickedOK = saveFileDialog1.ShowDialog();
                Log.Info($"Save dialog result: {userClickedOK}");

                // Process input if the user clicked OK.
                if (userClickedOK == true)
                {
                    string path = saveFileDialog1.FileName;
                    Log.Info($"User selected path: {path}");

                    using (Stream myStream = saveFileDialog1.OpenFile())
                    {
                        if (myStream != null)
                        {
                            Log.Info("Stream opened successfully. Preparing to save nodes...");

                            #region Nodes
                            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                            {
                                Indent = true,
                                IndentChars = "\t",
                                NewLineOnAttributes = true
                            };
                            Log.Info("Configured XmlWriterSettings for serialization.");

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

                            Log.Info("Serializing nodes...");

                            foreach (var node in Nodes)
                            {
                                if (node is RootNode rootNode)
                                {
                                    Log.Info($"Processing RootNode: {rootNode.NodeName}");

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

                                    Log.Info($"Serialized RootNode: {rootNode.NodeName}");
                                }
                                else if (node is ConditionNode conNode)
                                {
                                    Log.Info($"Processing ConditionNode: {conNode.NodeName}");

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

                                    Log.Info($"Serialized ConditionNode: {conNode.NodeName}");
                                }
                                else if (node is VariableNode varNode)
                                {
                                    Log.Info($"Processing VariableNode: {varNode.NodeName}");

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

                                    Log.Info($"Serialized VariableNode: {varNode.NodeName}");
                                }
                                else if (node is DynamicNode dynNode)
                                {
                                    Log.Info($"Processing DynamicNode: {dynNode.NodeName}");

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
                                        Log.Info($"Added argument: {arg.Name} to DynamicNode: {dynNode.NodeName}");
                                    }

                                    SerializeNodes.Add(dynSerial);
                                    Log.Info($"Serialized DynamicNode: {dynNode.NodeName}");
                                }
                            }

                            serializer.Serialize(writer, SerializeNodes);
                            Log.Info($"Nodes successfully serialized to path: {path}");

                            writer.Close();
                            Log.Info("TextWriter closed after serialization.");

                            myStream.Close();
                            Log.Info("Stream closed successfully.");

                            System.Windows.Clipboard.SetText(path);
                            Log.Info("Save completed. Path: {0} copied to clipboard. Success: {1}", path, true);
                            #endregion
                        }
                        else
                        {
                            Log.Info("Stream was null. Nodes were not saved.");
                        }
                    }
                }
                else
                {
                    Log.Info("User canceled the save dialog.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error during SaveNodes: {ex.Message}");
            }
        }



        public void LoadNodes()
        {
            Log.Info("Starting LoadNodes...");
            try
            {
                // Clear any existing nodes before loading new ones.
                Log.Info("Clearing existing nodes...");
                ClearNodes();
                Log.Info("Existing nodes cleared.");

                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                Log.Info("Opening file dialog for node XML selection...");
                bool? userClickedOK = openFileDialog1.ShowDialog();
                Log.Info($"File dialog result: {userClickedOK}");

                if (userClickedOK == true)
                {
                    string path = openFileDialog1.FileName;
                    Log.Info($"Selected file path: {path}");

                    List<SerializeableNodeViewModel> SerializeNodes;
                    XmlSerializer ser = new XmlSerializer(typeof(List<SerializeableNodeViewModel>),
                        new Type[] { typeof(SerializeableVariableNode), typeof(SerializeableConditionNode), typeof(SerializeableDynamicNode), typeof(SerializeableRootNode) });

                    using (XmlReader reader = XmlReader.Create(path))
                    {
                        Log.Info("Deserializing nodes from XML...");
                        SerializeNodes = (List<SerializeableNodeViewModel>)ser.Deserialize(reader);
                        Log.Info($"Deserialization complete. {SerializeNodes.Count} nodes found.");
                    }

                    // Add the deserialized nodes to the collection.
                    foreach (var serializeableNodeViewModel in SerializeNodes)
                    {
                        try
                        {
                            if (serializeableNodeViewModel is SerializeableRootNode rootSerialized)
                            {
                                Log.Info($"Processing SerializeableRootNode: {rootSerialized.NodeName}");
                                RootNode newNode = new RootNode(MainViewModel.Instance);
                                newNode.Populate(rootSerialized);
                                Nodes.Add(newNode);
                                Log.Info($"Added RootNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableVariableNode variableSerialized)
                            {
                                Log.Info($"Processing SerializeableVariableNode: {variableSerialized.NodeName}");
                                VariableNode newNode = new VariableNode(MainViewModel.Instance);
                                newNode.Populate(variableSerialized);
                                Nodes.Add(newNode);
                                Log.Info($"Added VariableNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableDynamicNode dynamicSerialized)
                            {
                                Log.Info($"Processing SerializeableDynamicNode: {dynamicSerialized.NodeName}");
                                DynamicNode newNode = new DynamicNode(MainViewModel.Instance);
                                newNode.Populate(dynamicSerialized);
                                Nodes.Add(newNode);
                                Log.Info($"Added DynamicNode: {newNode.NodeName}");
                            }
                            else if (serializeableNodeViewModel is SerializeableConditionNode conSerialized)
                            {
                                Log.Info($"Processing SerializeableConditionNode: {conSerialized.NodeName}");
                                ConditionNode newNode = new ConditionNode(MainViewModel.Instance);
                                newNode.Populate(conSerialized);
                                Nodes.Add(newNode);
                                Log.Info($"Added ConditionNode: {newNode.NodeName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Info($"Error while processing node: {ex.Message}");
                        }
                    }

                    // Connect nodes as per the saved configuration.
                    Log.Info("Connecting nodes...");
                    ConnectNodes();
                    Log.Info("Nodes connected successfully.");
                }
                else
                {
                    Log.Info("User canceled the file dialog. No nodes loaded.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error in LoadNodes: {ex.Message}");
            }
            Log.Info("LoadNodes process completed.");
        }

        private void ConnectNodes()
        {
            try
            {
                foreach (var node in Nodes)
                {
                    if (node is RootNode rootNode)
                    {
                        Log.Info($"Connecting output for RootNode: {rootNode.NodeName}");
                        if (rootNode.OutputConnector.ConnectionNodeID <= 0)
                        {
                            Log.Info("Output connection ID is not valid, skipping connection.");
                            continue;
                        }

                        Connector connectedTo = GetInConnectorBasedOnNode(rootNode.OutputConnector.ConnectionNodeID);
                        Connector.ConnectPins(rootNode.OutputConnector, connectedTo);
                        Log.Info($"Output connected for RootNode: {rootNode.NodeName}");
                    }

                    if (node is ConditionNode conNode)
                    {
                        Log.Info($"Connecting ConditionNode: {conNode.NodeName}");

                        if (conNode.boolInput.ConnectionNodeID > 0)
                        {
                            Connector connectedToVar = GetOutConnectorBasedOnNode(conNode.boolInput.ConnectionNodeID);
                            Connector.ConnectPins(conNode.boolInput, connectedToVar);
                            Log.Info($"Bool input connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.InExecutionConnector.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetOutConnectorBasedOnNode(conNode.InExecutionConnector.ConnectionNodeID);
                            Connector.ConnectPins(conNode.InExecutionConnector, connectedTo);
                            Log.Info($"Input connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.OutExecutionConnectorTrue.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorTrue.ConnectionNodeID);
                            Connector.ConnectPins(conNode.OutExecutionConnectorTrue, connectedTo);
                            Log.Info($"Output (True) connected for ConditionNode: {conNode.NodeName}");
                        }

                        if (conNode.OutExecutionConnectorFalse.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorFalse.ConnectionNodeID);
                            Connector.ConnectPins(conNode.OutExecutionConnectorFalse, connectedTo);
                            Log.Info($"Output (False) connected for ConditionNode: {conNode.NodeName}");
                        }
                    }

                    if (node is DynamicNode dynNode)
                    {
                        Log.Info($"Connecting DynamicNode: {dynNode.NodeName}");

                        for (int i = 0; i < dynNode.ArgumentCache.Count(); i++)
                        {
                            Argument arg = dynNode.ArgumentCache.ElementAt(i);

                            if (arg.ArgIsExistingVariable)
                            {
                                Connector conID = dynNode.GetConnectorAtIndex(i);
                                int connectedToVar = arg.ArgumentConnectedToNodeID;
                                Connector varConnect = GetOutConnectorBasedOnNode(connectedToVar);
                                Connector.ConnectPins(conID, varConnect);
                                Log.Info($"Argument connected for DynamicNode: {dynNode.NodeName}, Argument: {arg.Name}");
                            }
                        }

                        if (dynNode.OutExecutionConnector.ConnectionNodeID > 0)
                        {
                            Connector connectedTo = GetInConnectorBasedOnNode(dynNode.OutExecutionConnector.ConnectionNodeID);
                            Connector.ConnectPins(dynNode.OutExecutionConnector, connectedTo);
                            Log.Info($"Output connected for DynamicNode: {dynNode.NodeName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while connecting nodes: {ex.Message}");
            }
        }



        // Method to parse Lua script and create nodes dynamically
        public void ImportLuaFile(string filePath)
        {
            Log.Info($"Starting ImportLuaFile with filePath: {filePath}");
            try
            {
                // Read the Lua script from the specified file path
                string luaScript = File.ReadAllText(filePath);
                Log.Info("Lua script read successfully.");

                var lua = new Script();
                Log.Info("Lua script environment initialized.");

                // Load the script into the Lua environment (this will not execute the script)
                lua.LoadString(luaScript);
                Log.Info("Lua script loaded into environment.");

                // Parse functions
                Log.Info("Parsing functions from Lua script...");
                foreach (var global in lua.Globals.Keys)
                {
                    DynValue globalValue = lua.Globals.Get(global);
                    if (globalValue.Type == DataType.Function)
                    {
                        string functionName = global.ToString();
                        string functionBody = GetLuaFunctionBody(luaScript, functionName);
                        Log.Info($"Found function: {functionName}, adding as node.");
                        AddParsedNode(functionName, "Function", functionBody);
                        Log.Info($"Function '{functionName}' added as node.");
                    }
                }
                Log.Info("Function parsing completed.");

                // Parse conditionals
                Log.Info("Parsing conditionals...");
                ParseConditionals(luaScript);
                Log.Info("Conditionals parsing completed.");

                // Parse loops
                Log.Info("Parsing loops...");
                ParseLoops(luaScript);
                Log.Info("Loop parsing completed.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error in ImportLuaFile: {ex.Message}");
            }
        }

        // Method to parse conditionals like "if" and "else"
        private void ParseConditionals(string luaScript)
        {
            Log.Info("Starting ParseConditionals...");
            try
            {
                string ifPattern = @"if\s+\(.*?\)\s+then(.*?)end";
                var matches = Regex.Matches(luaScript, ifPattern, RegexOptions.Singleline);
                Log.Info($"Found {matches.Count} 'if' conditionals in the Lua script.");

                foreach (Match match in matches)
                {
                    string conditionBody = match.Groups[1].Value;
                    Log.Info("Adding 'If' conditional as node...");
                    AddParsedNode("Conditional", "If", conditionBody);
                    Log.Info("'If' conditional node added.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while parsing conditionals: {ex.Message}");
            }
            Log.Info("ParseConditionals completed.");
        }

        // Method to parse loops like "for" and "while"
        private void ParseLoops(string luaScript)
        {
            Log.Info("Starting ParseLoops...");
            try
            {
                // For-loop pattern
                string forPattern = @"for\s+.*?\s+in\s+.*?\s+do(.*?)end";
                var forMatches = Regex.Matches(luaScript, forPattern, RegexOptions.Singleline);
                Log.Info($"Found {forMatches.Count} 'for' loops in the Lua script.");

                foreach (Match match in forMatches)
                {
                    string loopBody = match.Groups[1].Value;
                    Log.Info("Adding 'For' loop as node...");
                    AddParsedNode("Loop", "For", loopBody);
                    Log.Info("'For' loop node added.");
                }

                // While-loop pattern
                string whilePattern = @"while\s+.*?\s+do(.*?)end";
                var whileMatches = Regex.Matches(luaScript, whilePattern, RegexOptions.Singleline);
                Log.Info($"Found {whileMatches.Count} 'while' loops in the Lua script.");

                foreach (Match match in whileMatches)
                {
                    string loopBody = match.Groups[1].Value;
                    Log.Info("Adding 'While' loop as node...");
                    AddParsedNode("Loop", "While", loopBody);
                    Log.Info("'While' loop node added.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while parsing loops: {ex.Message}");
            }
            Log.Info("ParseLoops completed.");
        }

        // Helper function to extract function body from Lua script by name
        private string GetLuaFunctionBody(string luaScript, string functionName)
        {
            Log.Info($"Extracting body for function: {functionName}");
            try
            {
                // Simple method to find and extract the function body for demonstration purposes
                string pattern = $@"function\s+{functionName}\s*\(.*?\)(.*?)end";
                var match = Regex.Match(luaScript, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    Log.Info($"Body extracted for function: {functionName}");
                    return match.Groups[1].Value;
                }
                else
                {
                    Log.Info($"No body found for function: {functionName}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error extracting function body for {functionName}: {ex.Message}");
                return string.Empty;
            }
        }


        public void ImportPerlFile(string filePath)
        {
            Log.Info($"Starting ImportPerlFile with filePath: {filePath}");
            try
            {
                // Define Perl script path and arguments
                string perlExecutable = @"C:\Strawberry\perl\bin\perl.exe";  // Path to your Perl executable
                string scriptPath = @"C:\Users\corys\Source\Repos\OR10N\FlowParser\parse_perl_script.pl";  // A Perl script that uses PPI to parse another Perl file

                Log.Info($"Perl executable: {perlExecutable}");
                Log.Info($"Script path: {scriptPath}");

                // Create a new process to execute the Perl script
                Process perlProcess = new Process();
                perlProcess.StartInfo.FileName = perlExecutable;
                perlProcess.StartInfo.Arguments = $"\"{scriptPath}\" \"{filePath}\"";  // Pass the file path as an argument
                perlProcess.StartInfo.RedirectStandardOutput = true;
                perlProcess.StartInfo.UseShellExecute = false;
                perlProcess.StartInfo.CreateNoWindow = true;

                Log.Info("Starting Perl process...");

                // Start the Perl script and capture the output
                perlProcess.Start();
                string output = perlProcess.StandardOutput.ReadToEnd();
                perlProcess.WaitForExit();
                Log.Info("Perl process completed. Output captured.");

                // Process the output in C#
                ParsePerlSubroutines(output);
                Log.Info("Perl subroutine parsing completed.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error in ImportPerlFile: {ex.Message}");
            }
        }

        // Method to parse the Perl script output (example)
        private void ParsePerlSubroutines(string output)
        {
            Log.Info("Parsing Perl subroutines from output...");
            try
            {
                // Here, you will split and parse the output from the Perl script
                // Assuming the output format is a list of subroutine names and bodies
                var subroutines = output.Split(new string[] { "Subroutine:" }, StringSplitOptions.RemoveEmptyEntries);
                Log.Info($"Found {subroutines.Length} subroutines.");

                foreach (var sub in subroutines)
                {
                    var lines = sub.Split('\n');
                    var name = lines[0].Trim();  // Subroutine name
                    var body = string.Join("\n", lines.Skip(1));  // Subroutine body

                    Log.Info($"Parsing subroutine: {name}");

                    // Create a new node in the flowchart for each subroutine
                    AddParsedNode(name, "Subroutine", body);
                    Log.Info($"Subroutine '{name}' added as a node.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while parsing Perl subroutines: {ex.Message}");
            }
        }

        // Finds children of a specific type in a visual tree
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            Log.Info($"Finding visual children of type: {typeof(T).Name}");
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    Log.Info($"Inspecting child at index {i}: {child?.GetType().Name ?? "null"}");

                    if (child != null && child is T)
                    {
                        Log.Info($"Found child of type {typeof(T).Name}");
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
            Log.Info($"Attempting to delete selected node: {node?.NodeName ?? "Unknown"}");
            try
            {
                if (node != null)
                {
                    Log.Info($"Disconnecting all connectors for node: {node.NodeName}");
                    node.DisconnectAllConnectors();
                    Nodes.Remove(node);
                    Log.Info($"Node '{node.NodeName}' removed from the collection.");
                }
                else
                {
                    Log.Info("No node selected for deletion.");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error while deleting node: {ex.Message}");
            }
        }


        public Connector GetInConnectorBasedOnNode(int nodeID)
        {
            Log.Info($"Attempting to find InConnector for node with ID: {nodeID}");

            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
            {
                Log.Info($"No node found with ID: {nodeID}. Returning null for InConnector.");
                return null;
            }

            try
            {
                if (node is DynamicNode dyn)
                {
                    Log.Info($"Node with ID: {nodeID} is a DynamicNode. Returning InConnector.");
                    return dyn.InConnector;
                }

                if (node is ConditionNode con)
                {
                    Log.Info($"Node with ID: {nodeID} is a ConditionNode. Returning InExecutionConnector.");
                    return con.InExecutionConnector;
                }

                Log.Info($"Node with ID: {nodeID} is not a recognized type for InConnector. Returning null.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while retrieving InConnector for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }

        public Connector GetOutConnectorBasedOnNode(int nodeID)
        {
            Log.Info($"Attempting to find OutConnector for node with ID: {nodeID}");

            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
            {
                Log.Info($"No node found with ID: {nodeID}. Returning null for OutConnector.");
                return null;
            }

            try
            {
                if (node is RootNode root)
                {
                    Log.Info($"Node with ID: {nodeID} is a RootNode. Returning OutputConnector.");
                    return root.OutputConnector;
                }

                if (node is DynamicNode dyn)
                {
                    Log.Info($"Node with ID: {nodeID} is a DynamicNode. Returning OutConnector.");
                    return dyn.OutConnector;
                }

                if (node is VariableNode varNode)
                {
                    Log.Info($"Node with ID: {nodeID} is a VariableNode. Returning NodeParameterOut.");
                    return varNode.NodeParameterOut;
                }

                Log.Info($"Node with ID: {nodeID} is not a recognized type for OutConnector. Returning null.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while retrieving OutConnector for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }

        public NodeViewModel GetNodeByID(int nodeID)
        {
            Log.Info($"Searching for node with ID: {nodeID}.");

            try
            {
                var node = from n in Nodes
                           where n.ID == nodeID
                           select n;

                if (node.Count() == 1)
                {
                    Log.Info($"Node with ID: {nodeID} found. Returning the node.");
                    return node.First();
                }

                Log.Info($"No unique node found with ID: {nodeID}. Found {node.Count()} matches. Returning null.");
            }
            catch (Exception ex)
            {
                Log.Info($"Error while searching for node with ID: {nodeID}: {ex.Message}");
            }

            return null;
        }


        public NetworkViewModel(MainViewModel mainViewModel)
{
    Log.Info("Initializing NetworkViewModel...");

    try
    {
        AddedNodesOrder = new List<NodeViewModel>();
        RemovedNodesOrder = new List<NodeViewModel>();
        Nodes = new ObservableCollection<NodeViewModel>();
        Log.Info("Node lists initialized.");

        // Initialize Undo/Redo commands
        Log.Info("Setting up Undo/Redo commands...");
        UndoCommand = new RelayCommand(Undo, CanUndo);
        RedoCommand = new RelayCommand(Redo, CanRedo);
        Log.Info("Undo/Redo commands initialized.");

        // Correctly instantiate NodeFactory with the MainViewModel singleton instance
        nodeFactory = new NodeFactory(MainViewModel.Instance);
        this.mainViewModel = mainViewModel;
        Log.Info("NodeFactory initialized and MainViewModel instance assigned.");

        Log.Info("NetworkViewModel initialized successfully.");
    }
    catch (Exception ex)
    {
        Log.Info($"Error during NetworkViewModel initialization: {ex.Message}");
    }
}

// Add a new undoable action to the stack
public void AddUndoableAction(UndoableAction action)
{
    Log.Info("Adding a new undoable action...");
    try
    {
        undoStack.Push(action);
        redoStack.Clear();  // Clear redo stack when new action is added
        RefreshCommandStates();
        Log.Info("Undoable action added successfully and redo stack cleared.");
    }
    catch (Exception ex)
    {
        Log.Info($"Error while adding undoable action: {ex.Message}");
    }
}

// Perform Undo
private void Undo()
{
    Log.Info("Attempting to perform Undo...");
    try
    {
        if (undoStack.Any())
        {
            var action = undoStack.Pop();
            Log.Info("Undo action found, executing Undo...");
            action.Undo();
            redoStack.Push(action);
            Log.Info("Undo executed successfully and action pushed to redo stack.");
            RefreshCommandStates();
        }
        else
        {
            Log.Info("No actions available to undo.");
        }
    }
    catch (Exception ex)
    {
        Log.Info($"Error during Undo operation: {ex.Message}");
    }
}

// Perform Redo
private void Redo()
{
    Log.Info("Attempting to perform Redo...");
    try
    {
        if (redoStack.Any())
        {
            var action = redoStack.Pop();
            Log.Info("Redo action found, executing Redo...");
            action.Execute();
            undoStack.Push(action);
            Log.Info("Redo executed successfully and action pushed to undo stack.");
            RefreshCommandStates();
        }
        else
        {
            Log.Info("No actions available to redo.");
        }
    }
    catch (Exception ex)
    {
        Log.Info($"Error during Redo operation: {ex.Message}");
    }
}

// Enable/Disable Undo button
private bool CanUndo()
{
    bool canUndo = undoStack.Any();
    Log.Info($"CanUndo check: {canUndo}");
    return canUndo;
}

// Enable/Disable Redo button
private bool CanRedo()
{
    bool canRedo = redoStack.Any();
    Log.Info($"CanRedo check: {canRedo}");
    return canRedo;
}

private void RefreshCommandStates()
{
    Log.Info("Refreshing command states for Undo/Redo...");
    try
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        Log.Info("Command states refreshed successfully.");
    }
    catch (Exception ex)
    {
        Log.Info($"Error while refreshing command states: {ex.Message}");
    }
}

public void RemoveNode(NodeViewModel node)
{
    Log.Info($"Attempting to remove node: {node?.NodeName ?? "Unknown"}");
    try
    {
        if (node != null && Nodes.Contains(node))
        {
            Nodes.Remove(node);
            Log.Info($"Node {node.NodeName} removed from the grid.");
        }
        else
        {
            Log.Info("Failed to remove node - node not found in the collection.");
        }
    }
    catch (Exception ex)
    {
        Log.Info($"Error while removing node: {ex.Message}");
    }
}


        }
    }




