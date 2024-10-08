using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CoffeeFlow.Annotations;
using CoffeeFlow.Nodes;
using CoffeeFlow.ViewModel;
using UnityFlow;
using static CoffeeFlow.ViewModel.NetworkViewModel;

namespace CoffeeFlow.Base


{
    public class NodeViewModel : UserControl, INotifyPropertyChanged
    {
        private double _scale;
        private readonly MainViewModel _mainViewModel;  // Reference to MainViewModel to add undoable actions

        public double Scale
        {
            get { return _scale; }
            set
            {
                double previousScale = _scale;
                _scale = value;
                OnPropertyChanged("Scale");

                // Add undoable action for scaling
                var undoAction = new UndoableAction(
                    doAction: () => Scale = _scale,
                    undoAction: () => Scale = previousScale
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        public NodeType NodeType
        {
            get { return _nodeType; }
            set
            {
                NodeType previousType = _nodeType;
                _nodeType = value;
                OnPropertyChanged("NodeType");

                // Add undoable action for node type change
                var undoAction = new UndoableAction(
                    doAction: () => NodeType = _nodeType,
                    undoAction: () => NodeType = previousType
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        private NodeType _nodeType;

        public virtual string GetSerializationString() => "";

        public string NodeDataString { get; set; }
        public string NodeDescription { get; set; }
        public bool CanDrag = true;

        private string callingClass;
        public string CallingClass
        {
            get { return callingClass; }
            set
            {
                string previousClass = callingClass;
                callingClass = value;
                OnPropertyChanged("CallingClass");

                // Add undoable action for calling class change
                var undoAction = new UndoableAction(
                    doAction: () => CallingClass = callingClass,
                    undoAction: () => CallingClass = previousClass
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        private int id;
        public int ID
        {
            get { return id; }
            set
            {
                int previousId = id;
                id = value;
                OnPropertyChanged("ID");

                // Add undoable action for ID change
                var undoAction = new UndoableAction(
                    doAction: () => ID = id,
                    undoAction: () => ID = previousId
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        public static int TotalIDCount = 0;

        public string NodeName
        {
            get { return _nodeName; }
            set
            {
                string previousName = _nodeName;
                _nodeName = value;
                OnPropertyChanged("NodeName");

                // Add undoable action for node name change
                var undoAction = new UndoableAction(
                    doAction: () => NodeName = _nodeName,
                    undoAction: () => NodeName = previousName
                );
                _mainViewModel.AddUndoableAction(undoAction);
            }
        }

        public string Debug { get; set; }

        public Double X { get; set; }
        public Double Y { get; set; }

        public bool IsDraggable = true;
        public TranslateTransform Transform { get; set; }
        public bool IsMouseDown;
        private string _nodeName;

        public static NodeViewModel Selected { get; set; }

        public static bool IsNodeDragging = false;
        public static double GlobalScaleDelta { get; set; }

        public ScaleTransform ScaleTransform { get; private set; }

        public virtual void Populate(SerializeableNodeViewModel node)
        {
            this.ID = node.ID;
            this.NodeName = node.NodeName;
            this.Margin = new Thickness(node.MarginX, node.MarginY, 0, 0);

            if (this.ID > TotalIDCount)
                TotalIDCount = this.ID;
        }

        public bool IsSelected
        {
            get => NodeViewModel.Selected != null && NodeViewModel.Selected == this;
        }

        public NodeViewModel(MainViewModel mainViewModel)
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            this.BorderBrush = new SolidColorBrush(Colors.Red);

            _mainViewModel = mainViewModel; // Initialize _mainViewModel first

            TotalIDCount++;
            ID = TotalIDCount;
            Scale = 1;
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

            // Add undoable action for scaling
            var undoAction = new UndoableAction(
                doAction: () => ScaleBy(increment),
                undoAction: () => ScaleBy(-increment)
            );
            _mainViewModel.AddUndoableAction(undoAction);
        }

        bool captured = false;
        UIElement source = null;

        public void MakeDraggable(UIElement moveThisElement, UIElement movedByElement)
        {
            ScaleTransform scaleTransform = new ScaleTransform(Scale, Scale);
            TranslateTransform transform = new TranslateTransform(0, 0);

            ScaleTransform = scaleTransform;

            TransformGroup group = new TransformGroup();
            group.Children.Add(scaleTransform);
            group.Children.Add(transform);

            moveThisElement.RenderTransform = group;

            this.Transform = transform;

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
            };

            movedByElement.MouseLeftButtonUp += (a, b) =>
            {
                if (captured)
                {
                    // Add undoable action for dragging
                    var undoAction = new UndoableAction(
                        doAction: () => Transform.X = transform.X,
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
                }
            };
        }

        public void DisconnectAllConnectors()
        {
            var connectors = FindVisualChildren<Connector>(this);

            foreach (var connector in connectors)
            {
                if (connector.IsConnected)
                {
                    connector.Connection.IsConnected = false;

                    if (connector.ParentNode.NodeType == NodeType.VariableNode)
                        connector.RemoveLinkedParameterFromVariableNode();
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
        }
    }
}
