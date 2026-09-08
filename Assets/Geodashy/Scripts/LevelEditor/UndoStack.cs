using System.Collections.Generic;

namespace Geodashy.Editing
{
    /// <summary>Snapshot-based undo/redo. Each entry is a serialised copy of the level contents.</summary>
    public class UndoStack
    {
        public int capacity = 60;
        readonly List<string> undo = new List<string>();
        readonly List<string> redo = new List<string>();

        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        public void Push(string snapshot)
        {
            undo.Add(snapshot);
            if (undo.Count > capacity) undo.RemoveAt(0);
            redo.Clear();
        }

        /// <summary>Removes the most recent undo entry (used when an interaction turned out to change nothing).</summary>
        public void DiscardLast()
        {
            if (undo.Count > 0) undo.RemoveAt(undo.Count - 1);
        }

        public string PopUndo(string current)
        {
            if (undo.Count == 0) return null;
            var s = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            redo.Add(current);
            return s;
        }

        public string PopRedo(string current)
        {
            if (redo.Count == 0) return null;
            var s = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            undo.Add(current);
            return s;
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }
    }
}
