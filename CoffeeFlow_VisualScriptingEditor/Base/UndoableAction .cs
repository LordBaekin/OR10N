using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OR10N
{ 
public class UndoableAction
{
    public Action DoAction { get; }
    public Action UndoAction { get; }

    public UndoableAction(Action doAction, Action undoAction)
    {
        DoAction = doAction;
        UndoAction = undoAction;
    }

    public void Execute() => DoAction();
        public void Undo() => UndoAction();

}
}
