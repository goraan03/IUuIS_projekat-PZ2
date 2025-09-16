using IUuIS_PZ2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IUuIS_PZ2.Services
{
    public class UndoManager
    {
        private readonly Stack<Action> _stack = new();
        public void Push(Action undo) => _stack.Push(undo);
        public void Undo() { if (_stack.Count > 0) _stack.Pop().Invoke(); }
        public ICommand UndoCommand => new RelayCommand(_ => Undo(), _ => _stack.Count > 0);
    }
}
