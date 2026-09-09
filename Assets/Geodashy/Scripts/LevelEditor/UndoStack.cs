using System.Collections.Generic;

namespace Geodashy.Editing
{
    /// <summary>Snapshot-based undo/redo. Each entry is a serialised copy of the level contents plus a short label.</summary>
    public class UndoStack
    {
        public struct Entry
        {
            public string snapshot;
            public string label;
        }

        public int capacity = 60;
        readonly List<Entry> undo = new List<Entry>();
        readonly List<Entry> redo = new List<Entry>();

        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        /// <summary>Oldest first: what undoing step N would revert.</summary>
        public IReadOnlyList<Entry> UndoEntries => undo;
        /// <summary>Most recently undone last.</summary>
        public IReadOnlyList<Entry> RedoEntries => redo;

        public void Push(string snapshot, string label = "Edit")
        {
            undo.Add(new Entry { snapshot = snapshot, label = string.IsNullOrEmpty(label) ? "Edit" : label });
            if (undo.Count > capacity) undo.RemoveAt(0);
            redo.Clear();
        }

        /// <summary>Removes the most recent undo entry (used when an interaction turned out to change nothing).</summary>
        public void DiscardLast()
        {
            if (undo.Count > 0) undo.RemoveAt(undo.Count - 1);
        }

        /// <summary>Label of the change an Undo would revert now.</summary>
        public string NextUndoLabel => undo.Count > 0 ? undo[undo.Count - 1].label : "";
        public string NextRedoLabel => redo.Count > 0 ? redo[redo.Count - 1].label : "";

        public string PopUndo(string current)
        {
            if (undo.Count == 0) return null;
            var e = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            redo.Add(new Entry { snapshot = current, label = e.label });
            return e.snapshot;
        }

        public string PopRedo(string current)
        {
            if (redo.Count == 0) return null;
            var e = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            undo.Add(new Entry { snapshot = current, label = e.label });
            return e.snapshot;
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }
    }
}
