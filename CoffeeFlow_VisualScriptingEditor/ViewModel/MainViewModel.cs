using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Win32;  // For OpenFileDialog
using CoffeeFlow.Annotations;
using CoffeeFlow.Base;
using CoffeeFlow.Nodes;
using CoffeeFlow.Views;
using Roslyn.Compilers.CSharp;
using UnityFlow;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using static CoffeeFlow.ViewModel.NetworkViewModel;

namespace CoffeeFlow.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private Stack<UndoableAction> undoStack = new Stack<UndoableAction>();
        private Stack<UndoableAction> redoStack = new Stack<UndoableAction>();

        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        private static MainViewModel instance = null;
        public static MainViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainViewModel();
                }
                return instance;
            }
        }

        private ObservableCollection<NodeWrapper> triggers = null;
        public ObservableCollection<NodeWrapper> Triggers
        {
            get
            {
                if (triggers == null)
                {
                    triggers = new ObservableCollection<NodeWrapper>();
                }

                return triggers;
            }
        }

        private ObservableCollection<NodeWrapper> methods = new ObservableCollection<NodeWrapper>();
        public ObservableCollection<NodeWrapper> Methods
        {
            get
            {
                if (methods == null)
                {
                    methods = new ObservableCollection<NodeWrapper>();
                }

                return methods;
            }
        }

        private ObservableCollection<NodeWrapper> variables = new ObservableCollection<NodeWrapper>();
        public ObservableCollection<NodeWrapper> Variables
        {
            get
            {
                if (variables == null)
                {
                    variables = new ObservableCollection<NodeWrapper>();
                }

                return variables;
            }
        }

        private ObservableCollection<LocalizationItem> localizationStrings = null;
        public ObservableCollection<LocalizationItem> LocalizationStrings
        {
            get
            {
                if (localizationStrings == null)
                {
                    localizationStrings = new ObservableCollection<LocalizationItem>();
                }

                return localizationStrings;
            }
        }

        public ObservableCollection<string> DebugList { get; set; }

        private string statusLabel = null;
        public string StatusLabel
        {
            get
            {
                return statusLabel;
            }
            set
            {
                statusLabel = value;
                RaisePropertyChanged("StatusLabel");
            }
        }

        private string fileLoadInfo = "";
        public string FileLoadInfo
        {
            get
            {
                return fileLoadInfo;
            }
            set
            {
                fileLoadInfo = value;
                RaisePropertyChanged("FileLoadInfo");
            }
        }
        private bool CanExecuteSaveAsLua()
        {
            bool canExecute = Nodes?.Any() == true;
            LogStatus($"CanExecuteSaveAsLua check: Nodes count = {Nodes?.Count ?? 0}, Can execute = {canExecute}");
            return canExecute;
        }

        private bool isClassFileName = true;
        public bool IsClassFileName
        {
            get
            {
                return isClassFileName;
            }
            set
            {
                isClassFileName = value;
                RaisePropertyChanged("IsClassFileName");
            }
        }

        private bool isAppend = true;
        public bool IsAppend
        {
            get
            {
                return isAppend;
            }
            set
            {
                isAppend = value;
                RaisePropertyChanged("IsAppend");
            }
        }
        private ObservableCollection<NodeWrapper> events = null;
        public ObservableCollection<NodeWrapper> Events
        {
            get
            {
                if (events == null)
                {
                    events = new ObservableCollection<NodeWrapper>();
                }
                return events;
            }
        }
        private ObservableCollection<NodeViewModel> nodes = new ObservableCollection<NodeViewModel>();
        public ObservableCollection<NodeViewModel> Nodes
        {
            get { return nodes; }
            set
            {
                nodes = value;
                RaisePropertyChanged(nameof(Nodes));
            }
        }

        private RelayCommand _saveAsLuaCommand;
        public RelayCommand SaveAsLuaCommand
        {
            get
            {
                LogStatus("Accessing SaveAsLuaCommand...");
                if (_saveAsLuaCommand == null)
                {
                    LogStatus("Initializing SaveAsLuaCommand...");
                    _saveAsLuaCommand = new RelayCommand(SaveAsLua, CanExecuteSaveAsLua);
                    LogStatus("SaveAsLuaCommand initialized.");
                }
                else
                {
                    LogStatus("SaveAsLuaCommand already initialized.");
                }
                return _saveAsLuaCommand;
            }
        }

        private RelayCommand _OpenCodeWindowCommand;
        public RelayCommand OpenCodeWindowCommand
        {
            get
            {
                LogStatus("Accessing OpenCodeWindowCommand...");
                if (_OpenCodeWindowCommand == null)
                {
                    LogStatus("Initializing OpenCodeWindowCommand...");
                    _OpenCodeWindowCommand = new RelayCommand(openCodeLoadPanelUI);
                    LogStatus("OpenCodeWindowCommand initialized.");
                }
                return _OpenCodeWindowCommand;
            }
        }

        private RelayCommand _OpenCodeFileFromFileCommand;
        public RelayCommand OpenCodeFileFromFileCommand
        {
            get
            {
                LogStatus("Accessing OpenCodeFileFromFileCommand...");
                if (_OpenCodeFileFromFileCommand == null)
                {
                    LogStatus("Initializing OpenCodeFileFromFileCommand...");
                    _OpenCodeFileFromFileCommand = new RelayCommand(openCodeFromFile);
                    LogStatus("OpenCodeFileFromFileCommand initialized.");
                }
                return _OpenCodeFileFromFileCommand;
            }
        }

        // Localization
        private RelayCommand _OpenLocalizationWindowCommand;
        public RelayCommand OpenLocalizationWindowCommand
        {
            get
            {
                LogStatus("Accessing OpenLocalizationWindowCommand...");
                if (_OpenLocalizationWindowCommand == null)
                {
                    LogStatus("Initializing OpenLocalizationWindowCommand...");
                    _OpenLocalizationWindowCommand = new RelayCommand(openLocalizationLoadPanelUI);
                    LogStatus("OpenLocalizationWindowCommand initialized.");
                }
                return _OpenLocalizationWindowCommand;
            }
        }

        private RelayCommand _OpenLocalizationFile;
        public RelayCommand OpenLocalizationFile
        {
            get
            {
                LogStatus("Accessing OpenLocalizationFile command...");
                if (_OpenLocalizationFile == null)
                {
                    LogStatus("Initializing OpenLocalizationFile command...");
                    _OpenLocalizationFile = new RelayCommand(OpenLocalization);
                    LogStatus("OpenLocalizationFile command initialized.");
                }
                return _OpenLocalizationFile;
            }
        }

        private string _newTriggerName = "Enter trigger name";
        public string NewTriggerName
        {
            get
            {
                LogStatus("Accessing NewTriggerName...");
                return _newTriggerName;
            }
            set
            {
                LogStatus($"Setting NewTriggerName to: {value}");
                _newTriggerName = value;
                RaisePropertyChanged("NewTriggerName");
                LogStatus("NewTriggerName changed.");
            }
        }

        private RelayCommand _AddTriggerCommand;
        public RelayCommand AddTriggerCommand
        {
            get
            {
                LogStatus("Accessing AddTriggerCommand...");
                if (_AddTriggerCommand == null)
                {
                    LogStatus("Initializing AddTriggerCommand...");
                    _AddTriggerCommand = new RelayCommand(AddNewTrigger);
                    LogStatus("AddTriggerCommand initialized.");
                }
                return _AddTriggerCommand;
            }
        }

        private RelayCommand<NodeWrapper> _DeleteNodeFromNodeListCommand;
        public RelayCommand<NodeWrapper> DeleteNodeFromNodeListCommand
        {
            get
            {
                LogStatus("Accessing DeleteNodeFromNodeListCommand...");
                if (_DeleteNodeFromNodeListCommand == null)
                {
                    LogStatus("Initializing DeleteNodeFromNodeListCommand...");
                    _DeleteNodeFromNodeListCommand = new RelayCommand<NodeWrapper>(DeleteNodeFromNodeList);
                    LogStatus("DeleteNodeFromNodeListCommand initialized.");
                }
                return _DeleteNodeFromNodeListCommand;
            }
        }


        public void AddNewTrigger()
        {
            try
            {
                LogStatus("Attempting to add new trigger.");
                if (!string.IsNullOrWhiteSpace(NewTriggerName))
                {
                    NodeWrapper newTrigger = new NodeWrapper
                    {
                        NodeName = NewTriggerName,
                        TypeOfNode = NodeType.RootNode
                    };

                    Triggers.Add(newTrigger);
                    LogStatus($"Added new trigger: {NewTriggerName}");

                    var undoAction = new UndoableAction(
                        doAction: () => Triggers.Add(newTrigger),
                        undoAction: () => Triggers.Remove(newTrigger)
                    );

                    AddUndoableAction(undoAction);
                    LogStatus("Undo action for trigger addition added.");

                    NewTriggerName = "";
                    RefreshCommandStates();
                    LogStatus("New trigger added successfully.");
                }
                else
                {
                    LogStatus("NewTriggerName is empty or whitespace. Cannot add trigger.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error while adding trigger: {ex.Message}");
            }
        }




        private void openLocalizationLoadPanelUI()
        {
            LogStatus("Opening Localization Load Panel UI...");
            try
            {
                OpenLocalizationData w = new OpenLocalizationData();
                w.ShowDialog();
                LogStatus("Localization Load Panel UI opened successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while opening Localization Load Panel UI: {ex.Message}");
            }
        }

        private void openCodeLoadPanelUI()
        {
            LogStatus("Opening Code Load Panel UI...");
            try
            {
                LogStatus("Ready to parse a C# code file", true);
                OpenCodeWindow w = new OpenCodeWindow();
                w.ShowDialog();
                LogStatus("Code Load Panel UI opened successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while opening Code Load Panel UI: {ex.Message}");
            }
        }

        private void openCodeFromFile()
        {
            LogStatus("Opening code from file...");
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "C# Files (.cs)|*.cs|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = true
                };

                bool? userClickedOK = openFileDialog1.ShowDialog();
                LogStatus($"File dialog result: {userClickedOK}");

                if (userClickedOK == true)
                {
                    int added = 0;
                    foreach (var file in openFileDialog1.FileNames)
                    {
                        OpenCode(file);
                        LogStatus($"Opened code file: {file}");
                        added++;
                    }
                    FileLoadInfo = $"Added {added} code file(s)";
                    LogStatus(FileLoadInfo, true);
                }
                else
                {
                    LogStatus("User canceled file open dialog.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error during code file opening: {ex.Message}");
            }
        }


        public void OpenCode(string file)
        {
            LogStatus($"Opening code file: {file}");
            try
            {
                string extension = System.IO.Path.GetExtension(file).ToLower();
                LogStatus($"File extension identified: {extension}");

                switch (extension)
                {
                    case ".cs":
                        GetMethods(file, isClassFileName);
                        GetVariables(file, isClassFileName);
                        LogStatus($"Processed C# file: {file}");
                        break;
                    case ".pl":
                        GetPerlMethods(file);
                        LogStatus($"Processed Perl file: {file}");
                        break;
                    case ".lua":
                        GetLuaMethods(file);
                        LogStatus($"Processed Lua file: {file}");
                        break;
                    case ".json":
                        GetJSONMethods(file);
                        LogStatus($"Processed JSON file: {file}");
                        break;
                    default:
                        LogStatus($"Unsupported file extension: {extension}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error while opening code file: {ex.Message}");
            }
        }



        private void OpenLocalization()
        {
            LogStatus("Opening localization file...");
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = "JSON Files (.json)|*.json|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                bool? userClickedOK = openFileDialog1.ShowDialog();
                LogStatus($"Localization file dialog result: {userClickedOK}");

                if (userClickedOK == true)
                {
                    string path = openFileDialog1.FileName;
                    LogStatus($"Reading localization file: {path}");
                    string json = File.ReadAllText(path);

                    LocalizationData data = JsonConvert.DeserializeObject<LocalizationData>(json);
                    LocalizationStrings.Clear();

                    foreach (var item in data.Items)
                    {
                        LocalizationStrings.Add(item);
                        LogStatus($"Added localization key: {item.Key}");
                    }

                    LogStatus($"Successfully parsed {LocalizationStrings.Count} localization keys", true);
                }
                else
                {
                    LogStatus("User canceled localization file dialog.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error while opening localization file: {ex.Message}");
            }
        }



        private RelayCommand _OpenDebug;
        public RelayCommand OpenDebugCommand
        {
            get { return _OpenDebug ?? (_OpenDebug = new RelayCommand(OpenDebug)); }
        }

        public void OpenDebug()
        {
            CodeResultWindow w = new CodeResultWindow();
            w.Show();
        }

        public void GetMethods(string filename, bool isClassNameOnly = false)
        {
            LogStatus($"Extracting methods from file: {filename}");
            try
            {
                string className = Path.GetFileNameWithoutExtension(filename);
                var syntaxTree = SyntaxTree.ParseFile(filename);
                var root = syntaxTree.GetRoot();
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                if (!classes.Any())
                {
                    LogStatus($"No classes found in file: {filename}.", true);
                    return;
                }

                foreach (var classNode in classes)
                {
                    string cname = classNode.Identifier.ToString();
                    if (isClassNameOnly && cname != className)
                    {
                        LogStatus($"Skipping class: {cname} as it does not match the file class name.");
                        continue;
                    }

                    LogStatus($"Found class: {cname}");

                    var methodDeclarations = classNode.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
                    foreach (var method in methodDeclarations)
                    {
                        bool isPublic = method.Modifiers.Any(mod => mod.Kind == SyntaxKind.PublicKeyword);
                        if (!isPublic)
                        {
                            LogStatus($"Skipping non-public method: {method.Identifier}");
                            continue;
                        }

                        NodeWrapper node = new NodeWrapper
                        {
                            NodeName = method.Identifier.ToString(),
                            CallingClass = cname,
                            TypeOfNode = NodeType.MethodNode
                        };

                        ParameterListSyntax parameters = method.ParameterList;
                        foreach (var param in parameters.Parameters)
                        {
                            Argument a = new Argument
                            {
                                Name = param.Identifier.ToString(),
                                ArgTypeString = param.Type.ToString()
                            };
                            node.Arguments.Add(a);
                            LogStatus($"Added parameter: {a.Name} of type: {a.ArgTypeString} to method: {node.NodeName}");
                        }

                        Methods.Add(node);
                        LogStatus($"Method added: {node.NodeName} with {node.Arguments.Count} parameters.");
                    }
                }

                LogStatus("Method extraction completed.");
            }
            catch (IOException ex)
            {
                LogStatus($"I/O error while reading file: {filename}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while extracting methods: {ex.Message}");
            }
        }



        public void GetPerlMethods(string filename)
        {
            LogStatus($"Extracting Perl methods from file: {filename}");
            try
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("sub"))
                    {
                        string functionName = line.Split(' ')[1].Trim(' ', '{');
                        NodeWrapper node = new NodeWrapper
                        {
                            NodeName = functionName,
                            TypeOfNode = NodeType.MethodNode
                        };
                        Methods.Add(node);
                        LogStatus($"Added Perl method: {functionName}");
                    }

                    var variableMatch = Regex.Match(line, @"([\$\@\%])(\w+)");
                    if (variableMatch.Success)
                    {
                        NodeWrapper node = new NodeWrapper
                        {
                            NodeName = variableMatch.Value,
                            TypeOfNode = NodeType.VariableNode
                        };
                        Variables.Add(node);
                        LogStatus($"Added Perl variable: {variableMatch.Value}");
                    }
                }

                LogStatus("Perl method extraction completed.");
            }
            catch (FileNotFoundException ex)
            {
                LogStatus($"Perl file not found: {filename}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while extracting Perl methods: {ex.Message}");
            }
        }

        private void SaveAsLua()
        {
            LogStatus("Executing SaveAsLua command...");
            try
            {
                string luaScript = ExportNodesToLua();
                LogStatus($"Lua script generated successfully. Script length: {luaScript.Length} characters.");

                // Save dialog for Lua script
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Lua Files (.lua)|*.lua|All Files (*.*)|*.*",
                    FilterIndex = 1
                };

                bool? result = saveFileDialog.ShowDialog();
                LogStatus($"Save dialog result: {result}");

                if (result == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, luaScript);
                    LogStatus($"Lua script saved to {saveFileDialog.FileName}");
                }
                else
                {
                    LogStatus("Save operation canceled by the user.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error during SaveAsLua execution: {ex.Message}");
            }
        }
        public string ExportNodesToLua()
        {
            StringBuilder luaScript = new StringBuilder();
            LogStatus("Starting export of nodes to Lua...");

            try
            {
                foreach (var node in Nodes)
                {
                    if (node is DynamicNode funcNode)
                    {
                        luaScript.AppendLine($"function {funcNode.NodeName}()");
                        luaScript.AppendLine(funcNode.NodeBody);  // Assuming NodeBody contains the Lua code for this node.
                        luaScript.AppendLine("end");
                        LogStatus($"Exported function: {funcNode.NodeName}");
                    }
                    // Add additional cases for exporting conditionals, loops, etc.
                }
                LogStatus("Node export to Lua completed successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error during Lua export: {ex.Message}");
            }

            return luaScript.ToString();
        }


        public void GetLuaMethods(string filename)
        {
            LogStatus($"Extracting Lua methods from file: {filename}");
            try
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("function"))
                    {
                        string functionName = line.Split(' ')[1].Split('(')[0];
                        NodeWrapper node = new NodeWrapper
                        {
                            NodeName = functionName,
                            TypeOfNode = NodeType.MethodNode
                        };
                        Methods.Add(node);
                        LogStatus($"Added Lua method: {functionName}");
                    }

                    var variableMatch = Regex.Match(line, @"(\w+)\s*=");
                    if (variableMatch.Success)
                    {
                        NodeWrapper node = new NodeWrapper
                        {
                            NodeName = variableMatch.Groups[1].Value,
                            TypeOfNode = NodeType.VariableNode
                        };
                        Variables.Add(node);
                        LogStatus($"Added Lua variable: {variableMatch.Groups[1].Value}");
                    }
                }
                LogStatus("Lua method extraction completed.");
            }
            catch (IOException ex)
            {
                LogStatus($"I/O error while reading Lua file: {filename}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while extracting Lua methods: {ex.Message}");
            }
        }



        public void GetSQLMethods(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("CREATE PROCEDURE") || line.Trim().StartsWith("CREATE FUNCTION"))
                {
                    string functionName = line.Split(' ')[2];
                    NodeWrapper node = new NodeWrapper
                    {
                        NodeName = functionName,
                        TypeOfNode = NodeType.MethodNode
                    };
                    Methods.Add(node);
                }

                var variableMatch = Regex.Match(line, @"DECLARE\s+@\w+");
                if (variableMatch.Success)
                {
                    NodeWrapper node = new NodeWrapper
                    {
                        NodeName = variableMatch.Value,
                        TypeOfNode = NodeType.VariableNode
                    };
                    Variables.Add(node);
                }

                if (line.Trim().StartsWith("CREATE TRIGGER"))
                {
                    string triggerName = line.Split(' ')[2];
                    NodeWrapper node = new NodeWrapper
                    {
                        NodeName = triggerName,
                        TypeOfNode = NodeType.EventNode
                    };
                    Events.Add(node);
                }
            }
        }

        public void GetJSONMethods(string filename)
        {
            LogStatus($"Extracting methods from JSON file: {filename}");
            try
            {
                string jsonContent = File.ReadAllText(filename);
                var jsonData = JsonConvert.DeserializeObject<List<string>>(jsonContent);

                foreach (var method in jsonData)
                {
                    NodeWrapper node = new NodeWrapper
                    {
                        NodeName = method,
                        TypeOfNode = NodeType.MethodNode
                    };
                    Methods.Add(node);
                    LogStatus($"Added method from JSON: {method}");
                }
                LogStatus("JSON method extraction completed.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while extracting JSON methods: {ex.Message}");
            }
        }


        public void GetVariables(string filename, bool isClassNameOnly = false)
        {
            LogStatus($"Extracting variables from file: {filename}");
            try
            {
                string className = System.IO.Path.GetFileNameWithoutExtension(filename);
                var syntaxTree = SyntaxTree.ParseFile(filename);
                var root = syntaxTree.GetRoot();

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDef in classes)
                {
                    string cname = classDef.Identifier.ToString();
                    if (isClassNameOnly && cname != className) continue;

                    LogStatus($"Found class: {cname}");

                    var variables = classDef.DescendantNodes().OfType<FieldDeclarationSyntax>();

                    foreach (var variable in variables)
                    {
                        var field = variable.Declaration;
                        var node = new NodeWrapper
                        {
                            NodeName = field.Variables.First().Identifier.ToString(),
                            CallingClass = cname,
                            BaseAssemblyType = field.Type.ToString(),
                            TypeOfNode = NodeType.VariableNode
                        };

                        bool isPublic = variable.Modifiers.Any(m => m.Kind == SyntaxKind.PublicKeyword);
                        if (isPublic)
                        {
                            Variables.Add(node);
                            LogStatus($"Added public variable: {node.NodeName} from class: {cname}");
                        }
                    }
                }

                LogStatus("Variable extraction completed.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while extracting variables: {ex.Message}");
            }
        }


        public void DeleteNodeFromNodeList(NodeWrapper node)
        {
            try
            {
                LogStatus($"Attempting to delete node: {node?.NodeName ?? "Unknown"}");

                if (node != null)
                {
                    // Remove node from the appropriate collection
                    if (node.TypeOfNode == NodeType.MethodNode)
                    {
                        Methods.Remove(node);
                        LogStatus($"Removed method node: {node.NodeName}");
                    }
                    else if (node.TypeOfNode == NodeType.ConditionNode || node.TypeOfNode == NodeType.RootNode)
                    {
                        Triggers.Remove(node);
                        LogStatus($"Removed trigger node: {node.NodeName}");
                    }
                    else if (node.TypeOfNode == NodeType.VariableNode)
                    {
                        Variables.Remove(node);
                        LogStatus($"Removed variable node: {node.NodeName}");
                    }

                    var undoAction = new UndoableAction(
                        doAction: () => DeleteNodeFromNodeList(node),
                        undoAction: () => AddNodeToList(node)
                    );

                    AddUndoableAction(undoAction);
                    LogStatus("Undo action for node deletion added.");

                    RefreshCommandStates();
                    LogStatus("Node deletion completed successfully.");
                }
                else
                {
                    LogStatus("Node is null. Cannot delete.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error while deleting node: {ex.Message}");
            }
        }




        private void AddNodeToList(NodeWrapper node)
        {
            try
            {
                LogStatus($"Adding node back to list: {node?.NodeName ?? "Unknown"}");
                if (node.TypeOfNode == NodeType.MethodNode)
                {
                    Methods.Add(node);
                    LogStatus($"Added method node: {node.NodeName} back to Methods.");
                }
                else if (node.TypeOfNode == NodeType.ConditionNode || node.TypeOfNode == NodeType.RootNode)
                {
                    Triggers.Add(node);
                    LogStatus($"Added trigger node: {node.NodeName} back to Triggers.");
                }
                else if (node.TypeOfNode == NodeType.VariableNode)
                {
                    Variables.Add(node);
                    LogStatus($"Added variable node: {node.NodeName} back to Variables.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error while adding node back to list: {ex.Message}");
            }
        }



        public MainViewModel()
        {
            // Initialization logic for DebugList
            DebugList = new ObservableCollection<string>();
            LogStatus("DebugList initialized.");

            LogStatus("Initializing MainViewModel...");

            try
            {
                // Ensure only one instance of MainViewModel exists
                if (instance != null && instance != this)
                {
                    LogStatus("Another instance of MainViewModel already exists. Throwing exception.");
                    throw new InvalidOperationException("An instance of MainViewModel already exists.");
                }

                instance = this;
                LogStatus("MainViewModel instance assigned.");

                // Initialize node wrappers
                LogStatus("Calling InitializeNodeWrappers...");
                InitializeNodeWrappers();
                LogStatus("Node wrappers initialized.");

                // Initialize Undo/Redo commands
                LogStatus("Setting up Undo/Redo commands...");
                UndoCommand = new RelayCommand(Undo, CanUndo);
                RedoCommand = new RelayCommand(Redo, CanRedo);
                LogStatus("Undo/Redo commands initialized.");

                LogStatus("MainViewModel initialization completed successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error during MainViewModel initialization: {ex.Message}");
                throw;  // Rethrow the exception to ensure it propagates after logging
            }
        }
       

        private void InitializeNodeWrappers()
        {
            LogStatus("Initializing node wrappers...");
            try
            {
                NodeWrapper r = new NodeWrapper
                {
                    TypeOfNode = NodeType.RootNode,
                    NodeName = "GameStart"
                };
                Triggers.Add(r);
                LogStatus("Added node: GameStart");

                NodeWrapper r2 = new NodeWrapper
                {
                    TypeOfNode = NodeType.RootNode,
                    NodeName = "Window Close"
                };
                Triggers.Add(r2);
                LogStatus("Added node: Window Close");

                NodeWrapper con = new NodeWrapper
                {
                    TypeOfNode = NodeType.ConditionNode,
                    NodeName = "Condition",
                    IsDeletable = false
                };
                Triggers.Add(con);
                LogStatus("Added node: Condition (undeletable)");

                LogStatus("Node wrappers initialized successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error during node wrapper initialization: {ex.Message}");
            }
        }



        public void AddUndoableAction(UndoableAction action)
        {
            LogStatus("Adding an undoable action...");
            try
            {
                if (action == null)
                {
                    LogStatus("Attempted to add a null undoable action.", true);
                    return;
                }

                undoStack.Push(action);
                LogStatus("Undo action added to the stack.");

                redoStack.Clear();  // Clear redo stack when a new action is added
                LogStatus("Redo stack cleared after adding a new action.");

                // Optional: Limit the size of the undo stack
                const int MAX_UNDO_ACTIONS = 50;
                if (undoStack.Count > MAX_UNDO_ACTIONS)
                {
                    undoStack = new Stack<UndoableAction>(undoStack.Take(MAX_UNDO_ACTIONS));
                    LogStatus($"Undo stack trimmed to maintain maximum size of {MAX_UNDO_ACTIONS} actions.");
                }

                RefreshCommandStates();
                LogStatus("Undoable action added successfully.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error while adding undoable action: {ex.Message}");
            }
        }




        public void Undo()
        {
            LogStatus("Attempting to undo last action...");
            try
            {
                if (undoStack.Any())
                {
                    var action = undoStack.Pop();
                    LogStatus("Popped action from the undo stack.");

                    action.Undo();
                    LogStatus("Action undone successfully.");

                    redoStack.Push(action);
                    LogStatus("Action pushed to the redo stack.");

                    RefreshCommandStates();
                    LogStatus("Command states refreshed after undo.");
                }
                else
                {
                    LogStatus("Undo action failed: No actions to undo.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error during undo operation: {ex.Message}");
            }
        }


        public void Redo()
        {
            LogStatus("Attempting to redo last undone action...");
            try
            {
                if (redoStack.Any())
                {
                    var action = redoStack.Pop();
                    LogStatus("Popped action from the redo stack.");

                    action.Execute();
                    LogStatus("Action re-executed successfully.");

                    undoStack.Push(action);
                    LogStatus("Action pushed back to the undo stack.");

                    RefreshCommandStates();
                    LogStatus("Command states refreshed after redo.");
                }
                else
                {
                    LogStatus("Redo action failed: No actions to redo.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error during redo operation: {ex.Message}");
            }
        }


        private bool CanUndo()
        {
            bool result = undoStack.Any();
            LogStatus($"CanUndo check: {result}");
            return result;
        }

        private bool CanRedo()
        {
            bool result = redoStack.Any();
            LogStatus($"CanRedo check: {result}");
            return result;
        }

        private void RefreshCommandStates()
        {
            LogStatus("Refreshing command states...");
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
            LogStatus("Command states refreshed.");
        }


        public void LogStatus(string message, bool showInStatusLabel = false, [CallerMemberName] string caller = "")
        {
            string logMessage = $"{DateTime.Now:HH:mm:ss} [{caller}] {message}";

            DebugList.Add(logMessage);

            if (showInStatusLabel)
            {
                StatusLabel = message;
            }

            Console.WriteLine(logMessage); // Optional: Output to console for debugging purposes.
        }



    }
}

