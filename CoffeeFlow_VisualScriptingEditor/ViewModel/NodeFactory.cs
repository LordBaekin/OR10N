using System;
using System.Windows;
using System.Windows.Input;
using OR10N.Nodes;
using OR10N.ViewModel;
using UnityFlow;
using FlowParser;  // Ensure this is imported for the Log class

namespace OR10N.Base
{
    public class NodeFactory
    {
        private MainViewModel mainViewModel;
        private Random random = new Random();

        public NodeFactory(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            Log.Info("NodeFactory initialized.");
        }

        public NodeViewModel CreateNode(NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating node of type: {nodeWrapper.TypeOfNode} with name: {nodeWrapper.NodeName}.");
            NodeViewModel nodeToAdd = null;

            try
            {
                Point p = DeterminePosition();
                Log.Info($"Position determined for new node: {p}.");

                switch (nodeWrapper.TypeOfNode)
                {
                    case NodeType.ActionNode:
                        nodeToAdd = CreateActionNode(p, nodeWrapper);
                        break;
                    case NodeType.EventNode:
                        nodeToAdd = CreateEventNode(p, nodeWrapper);
                        break;
                    case NodeType.RootNode:
                        nodeToAdd = CreateRootNode(p, nodeWrapper);
                        break;
                    case NodeType.ConditionNode:
                        nodeToAdd = CreateConditionNode(p, nodeWrapper);
                        break;
                    case NodeType.MethodNode:
                        nodeToAdd = CreateMethodNode(p, nodeWrapper);
                        break;
                    case NodeType.VariableNode:
                        nodeToAdd = CreateVariableNode(p, nodeWrapper);
                        break;
                    case NodeType.SetNode:
                        nodeToAdd = CreateSetNode(p, nodeWrapper);
                        break;
                    default:
                        Log.Warning($"Unsupported node type: {nodeWrapper.TypeOfNode}. Node creation skipped.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during node creation for {nodeWrapper.NodeName} of type {nodeWrapper.TypeOfNode}: {ex.Message}");
            }

            return nodeToAdd;
        }

        private Point DeterminePosition()
        {
            try
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                Point p = new Point(main.Width / 2, main.Height / 2);

                if (main.IsNodePopupVisible)
                {
                    p = Mouse.GetPosition(main);
                    Log.Info("Node popup is visible. Using mouse position for node placement.");
                }
                else
                {
                    int increment = random.Next(-400, 400);
                    p = new Point(p.X + increment, p.Y + increment);
                    Log.Info($"Node popup is not visible. Using random offset for node placement: {increment}.");
                }

                return p;
            }
            catch (Exception ex)
            {
                Log.Error($"Error determining position for new node: {ex.Message}");
                return new Point(0, 0); // Return a default position in case of error
            }
        }

        private ActionNode CreateActionNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating ActionNode at position {p} with name: {nodeWrapper.NodeName}.");
            ActionNode n = new ActionNode(mainViewModel, "SomeAction", "Some dialogue");
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private EventNode CreateEventNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating EventNode at position {p} with name: {nodeWrapper.NodeName}.");
            EventNode n = new EventNode(mainViewModel, "SomeEvent");
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private RootNode CreateRootNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating RootNode at position {p} with name: {nodeWrapper.NodeName}.");
            RootNode n = new RootNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private ConditionNode CreateConditionNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating ConditionNode at position {p} with name: {nodeWrapper.NodeName}.");
            ConditionNode n = new ConditionNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private SetNode CreateSetNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating SetNode at position {p} with name: {nodeWrapper.NodeName}.");
            SetNode n = new SetNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private DynamicNode CreateMethodNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating MethodNode at position {p} with name: {nodeWrapper.NodeName}.");
            DynamicNode n = new DynamicNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;

            try
            {
                foreach (var arg in nodeWrapper.Arguments)
                {
                    n.AddArgument(arg.ArgTypeString, arg.Name, false, 0, null);
                    Log.Info($"Added argument {arg.Name} of type {arg.ArgTypeString} to MethodNode: {nodeWrapper.NodeName}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding arguments to MethodNode {nodeWrapper.NodeName}: {ex.Message}");
            }

            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            n.CallingClass = nodeWrapper.CallingClass;
            return n;
        }

        private VariableNode CreateVariableNode(Point p, NodeWrapper nodeWrapper)
        {
            Log.Info($"Creating VariableNode at position {p} with name: {nodeWrapper.NodeName}.");
            VariableNode n = new VariableNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Type = nodeWrapper.BaseAssemblyType;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            n.CallingClass = nodeWrapper.CallingClass;
            return n;
        }
    }
}
