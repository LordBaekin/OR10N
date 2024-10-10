using System;
using System.Collections.Generic;
using FlowParser;
using GalaSoft.MvvmLight.Command;



namespace OR10N.FlowParser
{
    public class UndoRedoManager
    {
        // Stacks for undo and redo actions
        private readonly Stack<UndoableAction> undoStack = new Stack<UndoableAction>();
        private readonly Stack<UndoableAction> redoStack = new Stack<UndoableAction>();

        // RelayCommands for Undo/Redo
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        public UndoRedoManager()
        {
            // Initialize Undo and Redo commands
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);
        }

        // Add a new undoable action to the stack
        public void AddUndoableAction(UndoableAction action)
        {
            Log.Info("Adding a new undoable action...");
            try
            {
                undoStack.Push(action);
                redoStack.Clear();  // Clear redo stack when a new action is added
                RefreshCommandStates();
                Log.Info("Undoable action added successfully and redo stack cleared.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while adding undoable action: {ex.Message}");
            }
        }

        // Perform Undo
        public void Undo()
        {
            Log.Info("Attempting to perform Undo...");
            try
            {
                if (undoStack.Count > 0)
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
                    Log.Warning("No actions available to undo.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during Undo operation: {ex.Message}");
            }
        }

        // Perform Redo
        public void Redo()
        {
            Log.Info("Attempting to perform Redo...");
            try
            {
                if (redoStack.Count > 0)
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
                    Log.Warning("No actions available to redo.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during Redo operation: {ex.Message}");
            }
        }

        // Enable/Disable Undo button
        public bool CanUndo()
        {
            bool canUndo = undoStack.Count > 0;
            Log.Info($"CanUndo check: {canUndo}");
            return canUndo;
        }

        // Enable/Disable Redo button
        public bool CanRedo()
        {
            bool canRedo = redoStack.Count > 0;
            Log.Info($"CanRedo check: {canRedo}");
            return canRedo;
        }

        // Refresh the state of the Undo/Redo commands to enable/disable buttons
        public void RefreshCommandStates()
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
                Log.Error($"Error while refreshing command states: {ex.Message}");
            }
        }
    }

    // Define the UndoableAction class if not already defined
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
            DoAction?.Invoke();
        }

        public void Undo()
        {
            Log.Info("Undoing action in UndoableAction.");
            UndoAction?.Invoke();
        }
    }
}
