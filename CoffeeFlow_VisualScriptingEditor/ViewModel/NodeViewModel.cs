using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OR10N.Annotations;
using OR10N.Nodes;
using OR10N.ViewModel;
using UnityFlow;
using static OR10N.ViewModel.NetworkViewModel;

namespace OR10N.Base
{
    public class NodeViewModel : UserControl, INotifyPropertyChanged
    {
    private readonly MainViewModel _mainViewModel;

    protected MainViewModel MainViewModel => _mainViewModel;
        private double _scale;
        public double Scale
        {
            get => _scale;
            set
            {
                double previousScale = _scale;
                _scale = value;
                OnPropertyChanged("Scale");
                _mainViewModel.LogStatus($"Scale changed from {previousScale} to {_scale} for node {NodeName}");

                var undoAction = new UndoableAction(
                    _mainViewModel,
                    doAction: () => Scale = _scale,
                    undoAction: () => Scale = previousScale
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }


        private NodeType _nodeType;
        public NodeType NodeType
        {
            get => _nodeType;
            set
            {
                NodeType previousType = _nodeType;
                _nodeType = value;
                OnPropertyChanged("NodeType");
                _mainViewModel.LogStatus($"NodeType changed from {previousType} to {_nodeType} for node {NodeName}");

                var undoAction = new UndoableAction(
                    _mainViewModel,
                    doAction: () => NodeType = _nodeType,
                    undoAction: () => NodeType = previousType
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        public virtual string GetSerializationString() => "";

        public string NodeDataString { get; set; }
        public string NodeDescription { get; set; }
        public bool CanDrag = true;

        private string callingClass;
        public string CallingClass
        {
            get => callingClass;
            set
            {
                string previousClass = callingClass;
                callingClass = value;
                OnPropertyChanged("CallingClass");
                _mainViewModel.LogStatus($"CallingClass changed from {previousClass} to {callingClass} for node {NodeName}");

                var undoAction = new UndoableAction(
                    _mainViewModel,
                    doAction: () => CallingClass = callingClass,
                    undoAction: () => CallingClass = previousClass
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }


        private int id;
        public int ID
        {
            get => id;
            set
            {
                int previousId = id;
                id = value;
                OnPropertyChanged("ID");
                _mainViewModel.LogStatus($"ID changed from {previousId} to {id} for node {NodeName}");

                var undoAction = new UndoableAction(
                    _mainViewModel,
                    doAction: () => ID = id,
                    undoAction: () => ID = previousId
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }


        public static int TotalIDCount = 0;

        private string _nodeName;
        public string NodeName
        {
            get => _nodeName;
            set
            {
                string previousName = _nodeName;
                _nodeName = value;
                OnPropertyChanged("NodeName");
                _mainViewModel.LogStatus($"NodeName changed from {previousName} to {_nodeName}");

                var undoAction = new UndoableAction(
                    _mainViewModel,
                    doAction: () => NodeName = _nodeName,
                    undoAction: () => NodeName = previousName
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }


        public string Debug { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public bool IsDraggable = true;
        public TranslateTransform Transform { get; set; }
        public bool IsMouseDown;

        public static NodeViewModel Selected { get; set; }
        public static bool IsNodeDragging = false;
        public static double GlobalScaleDelta { get; set; }

        public ScaleTransform ScaleTransform { get; private set; }

        public virtual void Populate(SerializeableNodeViewModel node)
        {
            _mainViewModel.LogStatus($"Populating NodeViewModel with data: {node.NodeName}, ID: {node.ID}");
            ID = node.ID;
            NodeName = node.NodeName;
            Margin = new Thickness(node.MarginX, node.MarginY, 0, 0);

            if (ID > TotalIDCount)
                TotalIDCount = ID;

            _mainViewModel.LogStatus($"Populated NodeViewModel: {NodeName} with ID: {ID}");
        }

        public bool IsSelected => NodeViewModel.Selected != null && NodeViewModel.Selected == this;

        public NodeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            TotalIDCount++;
            ID = TotalIDCount;
            Scale = 1;
            _mainViewModel.LogStatus($"Creating NodeViewModel: {NodeName} with ID: {ID}");

            MakeDraggable(this, this);
            DataContext = this;

            ScaleBy(GlobalScaleDelta);
        }

        public void ScaleBy(double increment)
        {
            double previousScale = Scale;
            Scale += increment;
            ScaleTransform.ScaleX = Scale;
            ScaleTransform.ScaleY = Scale;

            _mainViewModel.LogStatus($"Scaled node {NodeName} from {previousScale} to {Scale} by {increment}");

            var undoAction = new UndoableAction(
                _mainViewModel,
                doAction: () => ScaleBy(increment),
                undoAction: () => ScaleBy(-increment)
            );
            _mainViewModel.AddUndoableAction(undoAction);
        }


        bool captured = false;
        UIElement source = null;

        public void MakeDraggable(UIElement moveThisElement, UIElement movedByElement)
        {
            _mainViewModel.LogStatus($"Making node {NodeName} draggable.");
            ScaleTransform scaleTransform = new ScaleTransform(Scale, Scale);
            TranslateTransform transform = new TranslateTransform(0, 0);

            ScaleTransform = scaleTransform;

            TransformGroup group = new TransformGroup();
            group.Children.Add(scaleTransform);
            group.Children.Add(transform);

            moveThisElement.RenderTransform = group;
            Transform = transform;

            Point originalPoint = new Point(0, 0), currentPoint;
            double initialX = 0, initialY = 0;

            movedByElement.MouseLeftButtonDown += (sender, b) =>
            {
                source = (UIElement)sender;
                Mouse.Capture(source);
                captured = true;

                IsNodeDragging = true;
                originalPoint = ((MouseEventArgs)b).GetPosition(moveThisElement);
                initialX = transform.X;
                initialY = transform.Y;

                NodeViewModel.Selected = this;
                OnPropertyChanged(nameof(IsSelected));
                _mainViewModel.LogStatus($"Node {NodeName} selected and drag started.");
            };

            movedByElement.MouseLeftButtonUp += (a, b) =>
            {
                if (captured)
                {
                    _mainViewModel.LogStatus($"Node {NodeName} drag ended. Final position: X={transform.X}, Y={transform.Y}");

                    var undoAction = new UndoableAction(
                        _mainViewModel,
                        doAction: () => { Transform.X = transform.X; Transform.Y = transform.Y; },
                        undoAction: () => { Transform.X = initialX; Transform.Y = initialY; }
                    );
                    _mainViewModel.AddUndoableAction(undoAction);
                }

                Mouse.Capture(null);
                captured = false;
                IsNodeDragging = false;
            };

            movedByElement.MouseMove += (a, b) =>
            {
                if (!IsDraggable) return;

                if (captured)
                {
                    currentPoint = ((MouseEventArgs)b).GetPosition(moveThisElement);
                    transform.X += currentPoint.X - originalPoint.X;
                    transform.Y += currentPoint.Y - originalPoint.Y;
                    _mainViewModel.LogStatus($"Node {NodeName} dragged to position: X={transform.X}, Y={transform.Y}");
                }
            };
        }

        public void DisconnectAllConnectors()
        {
            _mainViewModel.LogStatus($"Disconnecting all connectors for node {NodeName}");
            var connectors = FindVisualChildren<Connector>(this);

            foreach (var connector in connectors)
            {
                if (connector.IsConnected)
                {
                    connector.Connection.IsConnected = false;

                    if (connector.ParentNode.NodeType == NodeType.VariableNode)
                    {
                        connector.RemoveLinkedParameterFromVariableNode();
                        _mainViewModel.LogStatus($"Removed linked parameter from variable node {NodeName}");
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            _mainViewModel.LogStatus($"Property changed: {propertyName} for node {NodeName}");
        }
    }
}
