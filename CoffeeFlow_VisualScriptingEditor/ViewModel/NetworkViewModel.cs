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
using CoffeeFlow.Base;
using CoffeeFlow.Nodes;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UnityFlow;
using NLua;
using System.Text;
using System.Diagnostics;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight.Ioc;


namespace CoffeeFlow.ViewModel
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
                DoAction = doAction;
                UndoAction = undoAction;
            }

            public void Execute() => DoAction();
            public void Undo() => UndoAction();
        }

        private RelayCommand<NodeWrapper> _addNodeToGridCommand;
        public RelayCommand<NodeWrapper> AddNodeToGridCommand
        {
            get { return _addNodeToGridCommand ?? (_addNodeToGridCommand = new RelayCommand<NodeWrapper>(AddNodeToGrid)); }
        }


        private RelayCommand<LocalizationItem> _selectLocalizedStringCommand;
        public RelayCommand<LocalizationItem> SelectLocalizedStringCommand
        {
            get { return _selectLocalizedStringCommand ?? (_selectLocalizedStringCommand = new RelayCommand<LocalizationItem>(SelectLocalizedString)); }
        }

        private RelayCommand _SaveNodesCommand;
        public RelayCommand SaveNodesCommand
        {
            get { return _SaveNodesCommand ?? (_SaveNodesCommand = new RelayCommand(SaveNodes)); }
        }

        private RelayCommand _ClearNodesCommand;
        public RelayCommand ClearNodesCommand
        {
            get { return _ClearNodesCommand ?? (_ClearNodesCommand = new RelayCommand(ClearNodes)); }
        }


        private RelayCommand _LoadNodesCommand;
        public RelayCommand LoadNodesCommand
        {
            get { return _LoadNodesCommand ?? (_LoadNodesCommand = new RelayCommand(LoadNodes)); }
        }

        private RelayCommand<NodeViewModel> _deleteNodesCommand;
        public RelayCommand<NodeViewModel> DeleteNodesCommand
        {
            get { return _deleteNodesCommand ?? (_deleteNodesCommand = new RelayCommand<NodeViewModel>(DeleteSelectedNodes)); }
        }

        public DependencyObject MainWindow;
        public int BezierStrength = 80;

        private RelayCommand increaseBezier;
        public RelayCommand IncreaseBezierStrengthCommand
        {
            get { return increaseBezier ?? (increaseBezier = new RelayCommand(IncreaseBezier)); }
        }


        private RelayCommand decreaseBezier;
        public RelayCommand DecreaseBezierStrengthCommand
        {
            get { return decreaseBezier ?? (decreaseBezier = new RelayCommand(DecreaseBezier)); }
        }

        private RelayCommand resetBezier;
        public RelayCommand ResetBezierStrengthCommand
        {
            get { return resetBezier ?? (resetBezier = new RelayCommand(ResetBezier)); }
        }

        public void IncreaseBezier()
        {
            BezierStrength += 40;
        }

        public void DecreaseBezier()
        {
            BezierStrength -= 40;
        }

        public void ResetBezier()
        {
            BezierStrength = 80;
        }

        public void ConnectPinsFromSourceID(int sourceID, int destinationID)
        {
            Connector source = GetConnectorWithID(sourceID);
            Connector destination = GetConnectorWithID(destinationID);

            Connector.ConnectPins(source, destination);
        }

        public void SelectLocalizedString(LocalizationItem s)
        {
            System.Windows.Controls.TextBox t = DynamicNode.GetCurrentEditTextBox();
            if (t != null)
                t.Text = s.Key;
        }
        public string ExportNodesToLua()
        {
            StringBuilder luaScript = new StringBuilder();

            foreach (var node in this.Nodes)
            {
                if (node is DynamicNode)
                {
                    DynamicNode funcNode = (DynamicNode)node;
                    luaScript.AppendLine($"function {funcNode.NodeName}()");
                    luaScript.AppendLine(funcNode.NodeBody);
                    luaScript.AppendLine("end");
                }
                // Add additional cases for conditionals and loops
            }

            return luaScript.ToString();
        }

        public string ExportNodesToPerl()
        {
            StringBuilder perlScript = new StringBuilder();

            foreach (var node in this.Nodes)
            {
                if (node is DynamicNode)
                {
                    DynamicNode funcNode = (DynamicNode)node;
                    perlScript.AppendLine($"sub {funcNode.NodeName} {{");
                    perlScript.AppendLine(funcNode.NodeBody);
                    perlScript.AppendLine("}");
                }
                // Add additional cases for conditionals and loops
            }

            return perlScript.ToString();
        }
        

        // Modify the NetworkViewModel class to add nodes dynamically from parsed content.
        public void AddParsedNode(string nodeName, string nodeType, string body)
        {
            NodeViewModel nodeToAdd = null;

           

            // Create a new node for function, loop, or conditional
            switch (nodeType)
            {
                case "Function":
                    DynamicNode functionNode = new DynamicNode(mainViewModel); // Pass MainViewModel
                    functionNode.NodeName = nodeName;
                    functionNode.SetBody(body);
                    nodeToAdd = functionNode;
                    break;

                case "If":
                case "Conditional":
                    DynamicNode conditionalNode = new DynamicNode(mainViewModel); // Pass MainViewModel
                    conditionalNode.NodeName = "If statement";
                    nodeToAdd = conditionalNode;
                    break;

                case "Loop":
                    DynamicNode loopNode = new DynamicNode(mainViewModel); // Pass MainViewModel
                    loopNode.NodeName = "Loop: " + nodeName;
                    nodeToAdd = loopNode;
                    break;

                    // Add additional cases for other control structures as needed
            }

            if (nodeToAdd != null)
            {
                // Randomly position the new node on the grid
                Point p = GetRandomPosition();
                nodeToAdd.Margin = new Thickness(p.X, p.Y, 0, 0);

                // Add the node to the grid
                this.Nodes.Add(nodeToAdd);
            }
        }

        // Utility function to determine random node placement
        private Point GetRandomPosition()
        {
            MainWindow main = Application.Current.MainWindow as MainWindow;
            Random r = new Random();
            int increment = r.Next(-400, 400);
            return new Point(main.Width / 2 + increment, main.Height / 2 + increment);
        }

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            // Fetch the NetworkViewModel from the DataContext
            NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();

            // Create a new NodeWrapper (You can customize this based on your needs)
            NodeWrapper newNode = new NodeWrapper
            {
                NodeName = "New Node",  // Assign the name of the node
                TypeOfNode = NodeType.MethodNode, // Specify the type of node (adjust as necessary)
                Arguments = new List<Argument>(), // If arguments are required
                BaseAssemblyType = "System.String", // Example base assembly type
                CallingClass = "MyClass"  // Replace with the appropriate calling class name
            };

            // Use the existing AddNodeToGrid method to add the node to the grid
            network.AddNodeToGrid(newNode);  // Calls the existing logic to place the node

            // Optionally, show a confirmation message
            MessageBox.Show("Node added to the grid!");
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            // Fetch the network view model from the IoC container or DataContext
            NetworkViewModel network = SimpleIoc.Default.GetInstance<NetworkViewModel>();

            // Find the node that is selected (where IsSelected == true)
            var selectedNode = network.Nodes.FirstOrDefault(node => node.IsSelected);

            // Check if a node is selected for deletion
            if (selectedNode != null)
            {
                // Call the method to remove the selected node
                network.RemoveNode(selectedNode);

                // Provide user feedback
                MessageBox.Show($"Node '{selectedNode.NodeName}' deleted!");
            }
            else
            {
                // No node selected for deletion
                MessageBox.Show("No node selected to delete!");
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
            Connector connector = null;

            if (MainWindow == null)
                return null;

            var Connectors = FindVisualChildren<Connector>(MainWindow);

            foreach (Connector cp in Connectors)
            {
                if (cp.ID == id)
                {
                    return cp;
                }
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
            Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();

            // Set filter options and filter index.
            saveFileDialog1.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;

            string path = "";
            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = saveFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                path = saveFileDialog1.FileName;
                Stream myStream = saveFileDialog1.OpenFile();
                if (myStream != null)
                {
                    #region Nodes
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                    {
                        Indent = true,
                        IndentChars = "\t",
                        NewLineOnAttributes = true
                    };

                    List<SerializeableNodeViewModel> SerializeNodes = new List<SerializeableNodeViewModel>();

                    XmlSerializer serializer = new XmlSerializer(typeof(List<SerializeableNodeViewModel>), new Type[] { typeof(SerializeableVariableNode), typeof(SerializeableConditionNode), typeof(SerializeableRootNode), typeof(SerializeableDynamicNode) });
                    TextWriter writer = new StreamWriter(myStream);

                    foreach (var node in Nodes)
                    {
                        if (node is RootNode)
                        {
                            RootNode rootNode = node as RootNode;

                            SerializeableRootNode rootSerial = new SerializeableRootNode();
                            rootSerial.NodeName = rootNode.NodeName;
                            rootSerial.NodeType = rootSerial.NodeType;
                            rootSerial.MarginX = rootNode.Margin.Left + rootNode.Transform.X;
                            rootSerial.MarginY = rootNode.Margin.Top + rootNode.Transform.Y;
                            rootSerial.ID = rootNode.ID;
                            rootSerial.OutputNodeID = rootNode.OutputConnector.ConnectionNodeID; //connection to next node
                            rootSerial.CallingClass = rootNode.CallingClass;
                            SerializeNodes.Add(rootSerial);
                        }

                        if (node is ConditionNode)
                        {
                            ConditionNode conNode = node as ConditionNode;

                            SerializeableConditionNode conSerial = new SerializeableConditionNode();
                            conSerial.NodeName = conNode.NodeName;
                            conSerial.NodeType = conNode.NodeType;
                            conSerial.MarginX = conNode.Margin.Left + conNode.Transform.X;
                            conSerial.MarginY = conNode.Margin.Top + conNode.Transform.Y;
                            conSerial.ID = conNode.ID;
                            conSerial.InputNodeID = conNode.InExecutionConnector.ConnectionNodeID;
                            conSerial.OutputTrueNodeID = conNode.OutExecutionConnectorTrue.ConnectionNodeID;
                            conSerial.OutputFalseNodeID = conNode.OutExecutionConnectorFalse.ConnectionNodeID;
                            conSerial.BoolVariableID = conNode.boolInput.ConnectionNodeID;
                            conSerial.BoolVariableName = conNode.ConnectedToVariableName;
                            conSerial.BoolCallingClass = conNode.ConnectedToVariableCallerClassName;

                            SerializeNodes.Add(conSerial);
                        }

                        if (node is VariableNode)
                        {
                            VariableNode varNode = node as VariableNode;

                            SerializeableVariableNode varSerial = new SerializeableVariableNode();

                            varSerial.NodeName = varNode.NodeName;
                            varSerial.TypeString = varNode.Type;
                            varSerial.NodeType = varNode.NodeType;

                            varSerial.MarginX = varNode.Margin.Left + varNode.Transform.X;
                            varSerial.MarginY = varNode.Margin.Top + varNode.Transform.Y;
                            varSerial.ID = varNode.ID;

                            varSerial.ConnectedToNodeID = varNode.NodeParameterOut.ConnectionNodeID;
                            varSerial.ConnectedToConnectorID = varNode.NodeParameterOut.ConnectedToConnectorID;

                            varSerial.CallingClass = varNode.CallingClass;

                            SerializeNodes.Add(varSerial);
                        }

                        if (node is DynamicNode)
                        {
                            DynamicNode dynNode = node as DynamicNode;

                            SerializeableDynamicNode dynSerial = new SerializeableDynamicNode();

                            dynSerial.NodeType = dynNode.NodeType;
                            dynSerial.NodeName = dynNode.NodeName;
                            dynSerial.Command = dynNode.Command;
                            dynSerial.NodePanelHeight = dynNode.NodeHeight;
                            dynSerial.MarginX = dynNode.Margin.Left + dynNode.Transform.X;
                            dynSerial.MarginY = dynNode.Margin.Top + dynNode.Transform.Y;
                            dynSerial.ID = dynNode.ID;

                            foreach (var arg in dynNode.ArgumentCache)
                            {
                                dynSerial.Arguments.Add(arg);
                            }

                            dynSerial.InputNodeID = dynNode.InConnector.ConnectionNodeID;
                            dynSerial.OutputNodeID = dynNode.OutConnector.ConnectionNodeID;

                            dynSerial.CallingClass = dynNode.CallingClass;

                            SerializeNodes.Add(dynSerial);
                        }
                    }

                    serializer.Serialize(writer, SerializeNodes);
                    writer.Close();
                    myStream.Close();

                    System.Windows.Clipboard.SetText(path);
                    MainViewModel.Instance.LogStatus("Save completed. Path: " + path + " copied to clipboard", true);
                    #endregion
                }
            }
        }

        public void LoadNodes()
        {
            ClearNodes();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                List<SerializeableNodeViewModel> SerializeNodes = new List<SerializeableNodeViewModel>();

                string path = openFileDialog1.FileName;

                XmlSerializer ser = new XmlSerializer(typeof(List<SerializeableNodeViewModel>), new Type[] { typeof(SerializeableVariableNode), typeof(SerializeableConditionNode), typeof(SerializeableDynamicNode), typeof(SerializeableRootNode) });

                using (XmlReader reader = XmlReader.Create(path))
                {
                    SerializeNodes = (List<SerializeableNodeViewModel>)ser.Deserialize(reader);
                }

                //ADD NODES
                foreach (var serializeableNodeViewModel in SerializeNodes)
                {
                    if (serializeableNodeViewModel is SerializeableRootNode)
                    {
                        SerializeableRootNode rootSerialized = serializeableNodeViewModel as SerializeableRootNode;

                        RootNode newNode = new RootNode(MainViewModel.Instance);
                        newNode.Populate(rootSerialized);

                        Nodes.Add(newNode);
                    }

                    if (serializeableNodeViewModel is SerializeableVariableNode)
                    {
                        SerializeableVariableNode variableSerialized = serializeableNodeViewModel as SerializeableVariableNode;

                        VariableNode newNode = new VariableNode(MainViewModel.Instance);
                        newNode.Populate(variableSerialized);

                        Nodes.Add(newNode);
                    }

                    if (serializeableNodeViewModel is SerializeableDynamicNode)
                    {
                        SerializeableDynamicNode dynamicSerialized = serializeableNodeViewModel as SerializeableDynamicNode;

                        DynamicNode newNode = new DynamicNode(MainViewModel.Instance);
                        newNode.Populate(dynamicSerialized);

                        Nodes.Add(newNode);
                    }

                    if (serializeableNodeViewModel is SerializeableConditionNode)
                    {
                        SerializeableConditionNode conSerialized = serializeableNodeViewModel as SerializeableConditionNode;

                        ConditionNode newNode = new ConditionNode(MainViewModel.Instance);
                        newNode.Populate(conSerialized);

                        Nodes.Add(newNode);
                    }
                }
            }

            //Node Connections
            foreach (var node in Nodes)
            {
                if (node is RootNode)
                {
                    //Connect output
                    RootNode rootNode = node as RootNode;
                    if (rootNode.OutputConnector.ConnectionNodeID <= 0)
                        return;

                    //Connect this output to the connection's input
                    Connector connectedTo = GetInConnectorBasedOnNode(rootNode.OutputConnector.ConnectionNodeID);
                    Connector.ConnectPins(rootNode.OutputConnector, connectedTo);
                }

                if (node is ConditionNode)
                {
                    ConditionNode conNode = node as ConditionNode;

                    //bool value Input
                    if (conNode.boolInput.ConnectionNodeID > 0)
                    {
                        //we're connected to a parameter
                        Connector connectedToVar = GetOutConnectorBasedOnNode(conNode.boolInput.ConnectionNodeID); //variable
                        Connector.ConnectPins(conNode.boolInput, connectedToVar);

                    }

                    //Input
                    if (conNode.InExecutionConnector.ConnectionNodeID > 0)
                    {
                        Connector connectedTo = GetOutConnectorBasedOnNode(conNode.InExecutionConnector.ConnectionNodeID);
                        Connector.ConnectPins(conNode.InExecutionConnector, connectedTo);
                    }

                    //Ouput true
                    if (conNode.OutExecutionConnectorTrue.ConnectionNodeID > 0)
                    {
                        Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorTrue.ConnectionNodeID);
                        Connector.ConnectPins(conNode.OutExecutionConnectorTrue, connectedTo);
                    }

                    //Ouput false
                    if (conNode.OutExecutionConnectorFalse.ConnectionNodeID > 0)
                    {
                        Connector connectedTo = GetInConnectorBasedOnNode(conNode.OutExecutionConnectorFalse.ConnectionNodeID);
                        Connector.ConnectPins(conNode.OutExecutionConnectorFalse, connectedTo);
                    }
                }

                if (node is DynamicNode)
                {
                    //Connect output
                    DynamicNode dynNode = node as DynamicNode;

                    //Connect parameters
                    for (int i = 0; i < dynNode.ArgumentCache.Count(); i++)
                    {
                        Argument arg = dynNode.ArgumentCache.ElementAt(i);

                        if (arg.ArgIsExistingVariable)
                        {
                            Connector conID = dynNode.GetConnectorAtIndex(i);
                            int connectedToVar = arg.ArgumentConnectedToNodeID;

                            Connector varConnect = GetOutConnectorBasedOnNode(connectedToVar);
                            Connector.ConnectPins(conID, varConnect);
                        }
                    }

                    if (dynNode.OutExecutionConnector.ConnectionNodeID > 0)
                    {
                        //Connect this output to the connection's input
                        Connector connectedTo = GetInConnectorBasedOnNode(dynNode.OutExecutionConnector.ConnectionNodeID);
                        Connector.ConnectPins(dynNode.OutExecutionConnector, connectedTo);
                        //No need to connect this in to the connection's output so far.
                    }
                }
            }
        }


        // Method to parse Lua script and create nodes dynamically
        public void ImportLuaFile(string filePath)
        {
            string luaScript = File.ReadAllText(filePath);
            var lua = new Script();

            // Load the script, this will not execute the script
            lua.LoadString(luaScript);

            // Parse functions
            foreach (var global in lua.Globals.Keys)
            {
                DynValue globalValue = lua.Globals.Get(global);
                if (globalValue.Type == DataType.Function)
                {
                    string functionName = global.ToString();
                    string functionBody = GetLuaFunctionBody(luaScript, functionName);
                    AddParsedNode(functionName, "Function", functionBody);
                }
            }

            // Parse conditionals
            ParseConditionals(luaScript);

            // Parse loops
            ParseLoops(luaScript);
        }



        // Method to parse conditionals like "if" and "else"
        private void ParseConditionals(string luaScript)
        {
            string ifPattern = @"if\s+\(.*?\)\s+then(.*?)end";
            var matches = Regex.Matches(luaScript, ifPattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                string conditionBody = match.Groups[1].Value;
                AddParsedNode("Conditional", "If", conditionBody);
            }
        }

        // Method to parse loops like "for" and "while"
        private void ParseLoops(string luaScript)
        {
            // For-loop pattern
            string forPattern = @"for\s+.*?\s+in\s+.*?\s+do(.*?)end";
            var forMatches = Regex.Matches(luaScript, forPattern, RegexOptions.Singleline);

            foreach (Match match in forMatches)
            {
                string loopBody = match.Groups[1].Value;
                AddParsedNode("Loop", "For", loopBody);
            }

            // While-loop pattern
            string whilePattern = @"while\s+.*?\s+do(.*?)end";
            var whileMatches = Regex.Matches(luaScript, whilePattern, RegexOptions.Singleline);

            foreach (Match match in whileMatches)
            {
                string loopBody = match.Groups[1].Value;
                AddParsedNode("Loop", "While", loopBody);
            }
        }

        // Helper function to extract function body from Lua script by name
        private string GetLuaFunctionBody(string luaScript, string functionName)
        {
            // Simple method to find and extract the function body for demonstration purposes
            string pattern = $@"function\s+{functionName}\s*\(.*?\)(.*?)end";
            var match = Regex.Match(luaScript, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public void ImportPerlFile(string filePath)
        {
            // Define Perl script path and arguments
            string perlExecutable = @"C:\Strawberry\perl\bin\perl.exe";  // Path to your Perl executable
            string scriptPath = @"C:\Users\corys\Source\Repos\OR10N\FlowParser\parse_perl_script.pl";  // A Perl script that uses PPI to parse another Perl file

            // Create a new process to execute the Perl script
            Process perlProcess = new Process();
            perlProcess.StartInfo.FileName = perlExecutable;
            perlProcess.StartInfo.Arguments = $"\"{scriptPath}\" \"{filePath}\"";  // Pass the file path as an argument
            perlProcess.StartInfo.RedirectStandardOutput = true;
            perlProcess.StartInfo.UseShellExecute = false;
            perlProcess.StartInfo.CreateNoWindow = true;

            // Start the Perl script and capture the output
            perlProcess.Start();
            string output = perlProcess.StandardOutput.ReadToEnd();
            perlProcess.WaitForExit();

            // Process the output in C#
            ParsePerlSubroutines(output);
        }

        // Method to parse the Perl script output (example)
        private void ParsePerlSubroutines(string output)
        {
            // Here, you will split and parse the output from the Perl script
            // Assuming the output format is a list of subroutine names and bodies
            var subroutines = output.Split(new string[] { "Subroutine:" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sub in subroutines)
            {
                var lines = sub.Split('\n');
                var name = lines[0].Trim();  // Subroutine name
                var body = string.Join("\n", lines.Skip(1));  // Subroutine body

                // Create a new node in the flowchart for each subroutine
                AddParsedNode(name, "Subroutine", body);
            }
        }

        //finds children
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private ObservableCollection<NodeViewModel> nodes = null;
        public ObservableCollection<NodeViewModel> Nodes { get; set; }

        public static ObservableCollection<NodeViewModel> SelectedNodes { get; set; }

        public void DeleteSelectedNodes(NodeViewModel node)
        {
            if (node != null)
            {
                node.DisconnectAllConnectors();
                Nodes.Remove(node);
            }
        }

        public Connector GetInConnectorBasedOnNode(int nodeID)
        {
            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
                return null;

            if (node is DynamicNode)
            {
                var dyn = node as DynamicNode;
                return dyn.InConnector;
            }

            if (node is ConditionNode)
            {
                var con = node as ConditionNode;
                return con.InExecutionConnector;
            }

            return null;
        }

        public Connector GetOutConnectorBasedOnNode(int nodeID)
        {
            NodeViewModel node = GetNodeByID(nodeID);

            if (node == null)
                return null;

            if (node is RootNode)
            {
                var root = node as RootNode;
                return root.OutputConnector;
            }

            if (node is DynamicNode)
            {
                var dyn = node as DynamicNode;
                return dyn.OutConnector;
            }

            if (node is VariableNode)
            {
                var con = node as VariableNode;
                return con.NodeParameterOut;
            }

            return null;
        }

        public NodeViewModel GetNodeByID(int nodeID)
        {
            var node = from n in Nodes
                       where n.ID == nodeID
                       select n;

            if (node.Count() == 1)
            {
                return node.First();
            }

            return null;
        }

        public NetworkViewModel(MainViewModel mainViewModel)
        {
            AddedNodesOrder = new List<NodeViewModel>();
            RemovedNodesOrder = new List<NodeViewModel>();
            Nodes = new ObservableCollection<NodeViewModel>();

            // Initialize Undo/Redo commands
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);

            // Correctly instantiate NodeFactory with the MainViewModel singleton instance
            nodeFactory = new NodeFactory(MainViewModel.Instance);
            this.mainViewModel = mainViewModel;
        }

        // Add a new undoable action to the stack
        public void AddUndoableAction(UndoableAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();  // Clear redo stack when new action is added
        RefreshCommandStates();
    }

    // Perform Undo
    private void Undo()
    {
        if (undoStack.Any())
        {
            var action = undoStack.Pop();
            action.Undo();
            redoStack.Push(action);
            RefreshCommandStates();
        }
    }

    // Perform Redo
    private void Redo()
    {
        if (redoStack.Any())
        {
            var action = redoStack.Pop();
            action.Execute();
            undoStack.Push(action);
            RefreshCommandStates();
        }
    }

    // Enable/Disable Undo button
    private bool CanUndo() => undoStack.Any();

    // Enable/Disable Redo button
    private bool CanRedo() => redoStack.Any();

    private void RefreshCommandStates()
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
    }
        public void RemoveNode(NodeViewModel node)
        {
            if (node != null && Nodes.Contains(node))
            {
                Nodes.Remove(node);  // Remove the node from the collection
                MainViewModel.Instance.LogStatus($"Node {node.NodeName} removed from the grid.");
            }
            else
            {
                MainViewModel.Instance.LogStatus("Failed to remove node - node not found.");
            }

        }
    }
}



