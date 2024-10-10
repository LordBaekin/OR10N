using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowParser; // Ensure the Log class is available

namespace UnityFlow
{
    /**********************************************************************************************************
    *             A wrapper for nodes without the excessive data, used in the flow parser to represent nodes 
    *             as well as on the UI side of the tool to display the nodes in lists without fully instantiating an entire UI object
    *                                                      * Nick @ http://immersivenick.wordpress.com 
    *                                                      * Free for non-commercial use
    **********************************************************************************************************/
    public class NodeWrapper
    {
        public string NodeName { get; set; }
        public string NodeDescription { get; set; }
        public List<Argument> Arguments;
        public NodeType TypeOfNode;
        public string BaseAssemblyType { get; set; }
        public string CallingClass { get; set; }
        public bool IsDeletable { get; set; }

        public string DetailString
        {
            get
            {
                Log.Info("Accessing DetailString property.");
                return GetDetailStringBasedOnType();
            }
        }

        public string GetDetailStringBasedOnType()
        {
            Log.Info("Determining detail string based on node type.", nameof(GetDetailStringBasedOnType));
            string arg = "";

            try
            {
                switch (TypeOfNode)
                {
                    case NodeType.MethodNode:
                        Log.Info("NodeType is MethodNode.", nameof(GetDetailStringBasedOnType));
                        return " in " + CallingClass;

                    case NodeType.VariableNode:
                        Log.Info("NodeType is VariableNode.", nameof(GetDetailStringBasedOnType));
                        arg += BaseAssemblyType;
                        arg += " in " + CallingClass;
                        break;

                    case NodeType.ConditionNode:
                        Log.Info("NodeType is ConditionNode.", nameof(GetDetailStringBasedOnType));
                        return "True/false flow boolean condition";

                    case NodeType.RootNode:
                        Log.Info("NodeType is RootNode.", nameof(GetDetailStringBasedOnType));
                        return "Trigger";

                    default:
                        Log.Warning($"Unknown NodeType: {TypeOfNode}", nameof(GetDetailStringBasedOnType));
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while determining detail string: {ex.Message}", nameof(GetDetailStringBasedOnType));
            }

            Log.Info("Returning detail string.", nameof(GetDetailStringBasedOnType));
            return arg;
        }

        public NodeWrapper() : this(string.Empty)
        {
            Log.Info("Default constructor called.");
        }

        public NodeWrapper(string name)
        {
            Log.Info($"Constructor called with name: {name}");
            NodeName = name;
            Arguments = new List<Argument>();
            IsDeletable = true;

            Log.Info("NodeWrapper instance created.", nameof(NodeWrapper));
        }
    }
}
