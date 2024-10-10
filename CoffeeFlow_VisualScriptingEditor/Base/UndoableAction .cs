using System;
using OR10N.ViewModel;

namespace OR10N
{
    public class UndoableAction
    {
        private readonly MainViewModel _mainViewModel;
        public Action DoAction { get; }
        public Action UndoAction { get; }

        public UndoableAction(MainViewModel mainViewModel, Action doAction, Action undoAction)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel), "MainViewModel cannot be null.");
            _mainViewModel.LogStatus("Creating a new UndoableAction.");

            DoAction = doAction ?? throw new ArgumentNullException(nameof(doAction), "DoAction cannot be null.");
            UndoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction), "UndoAction cannot be null.");

            _mainViewModel.LogStatus("UndoableAction created successfully.");
        }

        public void Execute()
        {
            _mainViewModel.LogStatus("Executing UndoableAction.");
            try
            {
                DoAction();
                MainViewModel.Instance.RaiseCanExecuteChangedForSaveAsLua();
                _mainViewModel.LogStatus("UndoableAction executed successfully.");
            }
            catch (Exception ex)
            {
                _mainViewModel.LogStatus($"Error executing UndoableAction: {ex.Message}", true);
            }
        }

        public void Undo()
        {
            _mainViewModel.LogStatus("Undoing UndoableAction.");
            try
            {
                UndoAction();
                MainViewModel.Instance.RaiseCanExecuteChangedForSaveAsLua();
                _mainViewModel.LogStatus("UndoableAction undone successfully.");
            }
            catch (Exception ex)
            {
                _mainViewModel.LogStatus($"Error undoing UndoableAction: {ex.Message}", true);
            }
        }
    }
}
