using IUuIS_PZ2.Utils;
using System.Windows.Input;

namespace IUuIS_PZ2.Services
{
    public class UndoManager
    {
        private readonly Stack<Action> _stack = new();
        private readonly RelayCommand _undoCommand;

        public ICommand UndoCommand => _undoCommand;
        public int Count => _stack.Count;

        public UndoManager()
        {
            _undoCommand = new RelayCommand(_ => Undo(), _ => _stack.Count > 0);
        }

        public void Push(Action undo)
        {
            if (undo == null) return;
            _stack.Push(undo);
            _undoCommand.RaiseCanExecuteChanged();
        }

        public void Undo()
        {
            if (_stack.Count == 0) return;

            var action = _stack.Pop();
            try
            {
                action();
            }
            finally
            {
                _undoCommand.RaiseCanExecuteChanged();
            }
        }

        public void Clear()
        {
            _stack.Clear();
            _undoCommand.RaiseCanExecuteChanged();
        }
    }
}