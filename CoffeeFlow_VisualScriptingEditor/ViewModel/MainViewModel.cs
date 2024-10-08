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

namespace CoffeeFlow.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private static MainViewModel instance;
        public static MainViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new System.ArgumentException("Trying to access static MainViewModel instance while it has not been assigned yet");
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

        private ObservableCollection<NodeWrapper> methods = null;
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

        private ObservableCollection<NodeWrapper> variables = null;
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

        private RelayCommand _OpenCodeWindowCommand;
        public RelayCommand OpenCodeWindowCommand
        {
            get { return _OpenCodeWindowCommand ?? (_OpenCodeWindowCommand = new RelayCommand(openCodeLoadPanelUI)); }
        }

        private RelayCommand _OpenCodeFileFromFileCommand;
        public RelayCommand OpenCodeFileFromFileCommand
        {
            get { return _OpenCodeFileFromFileCommand ?? (_OpenCodeFileFromFileCommand = new RelayCommand(openCodeFromFile)); }
        }

        //Localization
        private RelayCommand _OpenLocalizationWindowCommand;
        public RelayCommand OpenLocalizationWindowCommand
        {
            get { return _OpenLocalizationWindowCommand ?? (_OpenLocalizationWindowCommand = new RelayCommand(openLocalizationLoadPanelUI)); }
        }

        private RelayCommand _OpenLocalizationFile;
        public RelayCommand OpenLocalizationFile
        {
            get { return _OpenLocalizationFile ?? (_OpenLocalizationFile = new RelayCommand(OpenLocalization)); }
        }

        private string _newTriggerName = "Enter trigger name";
        public string NewTriggerName
        {
            get { return _newTriggerName; }
            set
            {
                _newTriggerName = value;
                RaisePropertyChanged("NewTriggerName");
            }
        }

        private RelayCommand _AddTriggerCommand;
        public RelayCommand AddTriggerCommand
        {
            get { return _AddTriggerCommand ?? (_AddTriggerCommand = new RelayCommand(AddNewTrigger)); }
        }

        private RelayCommand<NodeWrapper> _DeleteNodeFromNodeListCommand;
        public RelayCommand<NodeWrapper> DeleteNodeFromNodeListCommand
        {
            get { return _DeleteNodeFromNodeListCommand ?? (_DeleteNodeFromNodeListCommand = new RelayCommand<NodeWrapper>(DeleteNodeFromNodeList)); }
        }

        public void AddNewTrigger()
        {
            if (NewTriggerName != "")
            {
                NodeWrapper newTrigger = new NodeWrapper();
                newTrigger.NodeName = NewTriggerName;
                newTrigger.TypeOfNode = NodeType.RootNode;

                Triggers.Add(newTrigger);

                NewTriggerName = "";
            }
        }

        private void openLocalizationLoadPanelUI()
        {
            OpenLocalizationData w = new OpenLocalizationData();
            w.ShowDialog();
        }

        private void openCodeLoadPanelUI()
        {
            LogStatus("Ready to parse a C# code file", true);
            OpenCodeWindow w = new OpenCodeWindow();
            w.ShowDialog();
        }

        private void openCodeFromFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();  // Fully qualified name

            // Set filter options and filter index.
            openFileDialog1.Filter = "C# Files (.cs)|*.cs|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = openFileDialog1.ShowDialog();

            int added = 0;
            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                foreach (var file in openFileDialog1.FileNames)
                {
                    OpenCode(file);
                    added++;
                }

                FileLoadInfo = "Added " + added + " code file(s)";
            }
        }

        public void OpenCode(string file)
        {
            string extension = System.IO.Path.GetExtension(file).ToLower();  // Correct Path usage

            // Decide how to parse based on the file type
            switch (extension)
            {
                case ".cs":
                    GetMethods(file, isClassFileName);
                    GetVariables(file, isClassFileName);
                    break;
                case ".pl":
                    GetPerlMethods(file);  // Perl support
                    break;
                case ".lua":
                    GetLuaMethods(file);   // Lua support
                    break;
                case ".json":
                    GetJSONMethods(file);  // JSON support
                    break;
                default:
                    LogStatus($"Unsupported file extension: {extension}");
                    break;
            }
        }

        private void OpenLocalization()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();  // Fully qualified

            openFileDialog1.Filter = "JSON Files (.json)|*.json|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;

            bool? userClickedOK = openFileDialog1.ShowDialog();

            if (userClickedOK == true)
            {
                string path = openFileDialog1.FileName;
                string json = File.ReadAllText(path);
                try
                {
                    LocalizationData data = JsonConvert.DeserializeObject<LocalizationData>(json);
                    LocalizationStrings.Clear();

                    foreach (var item in data.Items)
                    {
                        LocalizationStrings.Add(item);
                    }

                    LogStatus("Successfully parsed " + LocalizationStrings.Count + " localization keys", true);
                }
                catch (Exception e)
                {
                    LogStatus("JSON File is not in the correct format", true);
                    throw;
                }
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
            string className = System.IO.Path.GetFileNameWithoutExtension(filename);  // Correct Path usage
            var syntaxTree = SyntaxTree.ParseFile(filename);
            var root = syntaxTree.GetRoot();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classes)
            {
                string cname = classNode.Identifier.ToString();

                // Skip this class entirely if it doesn't match the class name of the code file
                if (isClassNameOnly && cname != className)
                    continue;

                // Avoid duplicate local variable 'methods'
                IEnumerable<MethodDeclarationSyntax> methodDeclarations = classNode
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .ToList();

                foreach (var method in methodDeclarations)
                {
                    NodeWrapper node = new NodeWrapper();
                    node.NodeName = method.Identifier.ToString();
                    node.CallingClass = cname;

                    bool isPublic = false;
                    foreach (var mod in method.Modifiers)
                    {
                        if (mod.Kind == SyntaxKind.PublicKeyword)
                        {
                            isPublic = true;
                        }
                    }

                    if (!isPublic)
                        continue;

                    ParameterListSyntax parameters = method.ParameterList;

                    foreach (var param in parameters.Parameters)
                    {
                        Argument a = new Argument
                        {
                            Name = param.Identifier.ToString(),
                            ArgTypeString = param.Type.ToString()
                        };
                        node.Arguments.Add(a);
                    }

                    node.TypeOfNode = NodeType.MethodNode;
                    Methods.Add(node);
                }
            }
        }

        public void GetPerlMethods(string filename)
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
                }
            }
        }

        public void GetLuaMethods(string filename)
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
                }
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
            string jsonContent = File.ReadAllText(filename);

            // Deserialize into a List<string> since the JSON is an array
            var jsonData = JsonConvert.DeserializeObject<List<string>>(jsonContent);

            foreach (var method in jsonData)
            {
                NodeWrapper node = new NodeWrapper
                {
                    NodeName = method,  // Assign the method string
                    TypeOfNode = NodeType.MethodNode
                };
                Methods.Add(node);
            }
        }

        public void GetVariables(string filename, bool isClassNameOnly = false)
        {
            string className = System.IO.Path.GetFileNameWithoutExtension(filename);
            var syntaxTree = SyntaxTree.ParseFile(filename);
            var root = syntaxTree.GetRoot();

            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDef in classes)
            {
                string cname = classDef.Identifier.ToString();

                if (isClassNameOnly && cname != className)
                    continue;

                IEnumerable<FieldDeclarationSyntax> variables = classDef
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .ToList();

                foreach (var variable in variables)
                {
                    FieldDeclarationSyntax field = variable;
                    VariableDeclarationSyntax var = field.Declaration;

                    NodeWrapper node = new NodeWrapper
                    {
                        NodeName = var.Variables.First().Identifier.ToString(),
                        CallingClass = cname,
                        BaseAssemblyType = var.Type.ToString(),
                        TypeOfNode = NodeType.VariableNode
                    };

                    bool isPublic = false;
                    foreach (var mod in field.Modifiers)
                    {
                        if (mod.Kind == SyntaxKind.PublicKeyword)
                        {
                            isPublic = true;
                        }
                    }

                    if (isPublic)
                        Variables.Add(node);
                }
            }
        }

        public void DeleteNodeFromNodeList(NodeWrapper node)
        {
            if (node.TypeOfNode == NodeType.MethodNode)
                Methods.Remove(node);

            if (node.TypeOfNode == NodeType.ConditionNode || node.TypeOfNode == NodeType.RootNode)
                Triggers.Remove(node);

            if (node.TypeOfNode == NodeType.VariableNode)
                Variables.Remove(node);
        }

        public MainViewModel()
        {
            if (instance != null && instance != this)
                LogStatus("There's already a Game Manager in the scene, destroying this one.");
            else
                instance = this;

            DebugList = new ObservableCollection<string>();
            LogStatus("MainViewModel initialized.");

            NodeWrapper r = new NodeWrapper
            {
                TypeOfNode = NodeType.RootNode,
                NodeName = "GameStart"
            };

            NodeWrapper r2 = new NodeWrapper
            {
                TypeOfNode = NodeType.RootNode,
                NodeName = "Window Close"
            };

            NodeWrapper con = new NodeWrapper
            {
                TypeOfNode = NodeType.ConditionNode,
                NodeName = "Condition",
                IsDeletable = false
            };

            Triggers.Add(r);
            Triggers.Add(r2);
            Triggers.Add(con);
        }

        public void LogStatus(string status, bool showInStatusLabel = false)
        {
            DebugList.Add(status);

            if (showInStatusLabel)
                StatusLabel = status;
        }
    }
}
