using System;
using System.Windows;
using System.Windows.Input;
using CoffeeFlow.Nodes;
using CoffeeFlow.ViewModel;
using UnityFlow;

namespace CoffeeFlow.Base
{
    public class NodeFactory
    {
        private MainViewModel mainViewModel;
        private Random random = new Random();

        public NodeFactory(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        public NodeViewModel CreateNode(NodeWrapper nodeWrapper)
        {
            NodeViewModel nodeToAdd = null;

            Point p = DeterminePosition();

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
            }

            return nodeToAdd;
        }

        private Point DeterminePosition()
        {
            MainWindow main = Application.Current.MainWindow as MainWindow;
            Point p = new Point(main.Width / 2, main.Height / 2);

            if (main.IsNodePopupVisible)
                p = Mouse.GetPosition(main);
            else
            {
                int increment = random.Next(-400, 400);
                p = new Point(p.X + increment, p.Y + increment);
            }

            return p;
        }

        private ActionNode CreateActionNode(Point p, NodeWrapper nodeWrapper)
        {
            ActionNode n = new ActionNode(mainViewModel, "SomeAction", "Some dialogue");
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private EventNode CreateEventNode(Point p, NodeWrapper nodeWrapper)
        {
            EventNode n = new EventNode(mainViewModel, "SomeEvent");
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private RootNode CreateRootNode(Point p, NodeWrapper nodeWrapper)
        {
            RootNode n = new RootNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private ConditionNode CreateConditionNode(Point p, NodeWrapper nodeWrapper)
        {
            ConditionNode n = new ConditionNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private SetNode CreateSetNode(Point p, NodeWrapper nodeWrapper)
        {
            SetNode n = new SetNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            return n;
        }

        private DynamicNode CreateMethodNode(Point p, NodeWrapper nodeWrapper)
        {
            DynamicNode n = new DynamicNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;

            foreach (var arg in nodeWrapper.Arguments)
            {
                n.AddArgument(arg.ArgTypeString, arg.Name, false, 0, null);
            }

            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            n.CallingClass = nodeWrapper.CallingClass;
            return n;
        }

        private VariableNode CreateVariableNode(Point p, NodeWrapper nodeWrapper)
        {
            VariableNode n = new VariableNode(mainViewModel);
            n.NodeName = nodeWrapper.NodeName;
            n.Type = nodeWrapper.BaseAssemblyType;
            n.Margin = new Thickness(p.X, p.Y, 0, 0);
            n.CallingClass = nodeWrapper.CallingClass;
            return n;
        }
    }
}
