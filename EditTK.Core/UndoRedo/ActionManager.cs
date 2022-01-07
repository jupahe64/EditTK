using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.UndoRedo
{
    /// <summary>
    /// Keeps track of the action/state history and exposes methods for undo and redo
    /// </summary>
    public sealed class ActionManager
    {
        /// <summary>
        /// Gets triggered the saved state changed
        /// </summary>
        public event EventHandler? SavedChanged;

        /// <summary>
        /// Gets triggered when the current state changed
        /// </summary>
        public event EventHandler? StateChanged;

        private readonly Stack<RevertableAction> _undoStack = new Stack<RevertableAction>();
        private readonly Stack<RevertableAction> _redoStack = new Stack<RevertableAction>();

        private int _savedActionIndex = 0;
        private int _currentStateIndex = 0;

        private int _collectionNestingLevel = 0;

        private List<RevertableAction>? _currentCollection = null;

        /// <summary>
        /// Undoes the last action/revertable collection
        /// </summary>
        public void Undo()
        {
            HandleOpenRevertableCollection();

            _redoStack.Push(_undoStack.Pop().Revert());
            _currentStateIndex--;

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Redoes the last action/revertable collection
        /// </summary>
        public void Redo()
        {
            HandleOpenRevertableCollection();

            _undoStack.Push(_redoStack.Pop().Revert());
            _currentStateIndex++;

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Begins a revertable collection where all submitted actions count as one. Can be stacked/nested without any side effcts.
        /// </summary>
        public void BeginCollection()
        {
            if (_collectionNestingLevel == 0)
                _currentCollection = new List<RevertableAction>();

            _collectionNestingLevel++;
        }

        /// <summary>
        /// Ends the current revertable collection if there is one. Can be stacked/nested without any side effcts
        /// </summary>
        public void EndCollection()
        {
            if (_currentCollection == null)
                return;

            _collectionNestingLevel--;

            if (_collectionNestingLevel == 0)
            {
                var action = new RevertableCollection(_currentCollection.ToArray());

                _currentCollection = null;

                SubmitActionInternal(action);
            }
        }

        /// <summary>
        /// Submits a <see cref="RevertableAction"/> to the action history
        /// </summary>
        /// <param name="action"></param>
        public void SubmitAction(RevertableAction action)
        {
            if (_currentCollection == null)
                SubmitActionInternal(action);
            else
                _currentCollection.Add(action);
        }

        private void SubmitActionInternal(RevertableAction action, bool suppressStateChangeEvent = false)
        {
            _redoStack.Clear();
            _undoStack.Push(action);
            if (_savedActionIndex > _currentStateIndex)
            {
                bool wasSaved = IsSaved();

                _savedActionIndex = -1; //the saved state is now lost forever

                if (wasSaved)
                    SavedChanged?.Invoke(this, EventArgs.Empty);
            }

            _currentStateIndex++;

            if(!suppressStateChangeEvent)
                StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Marks the current state as the saved state
        /// </summary>
        public void SetSaved()
        {
            HandleOpenRevertableCollection();

            bool wasSaved = IsSaved();

            _savedActionIndex = _currentStateIndex;

            if (!wasSaved)
                SavedChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Checks if the current state is the saved state
        /// </summary>
        /// <returns></returns>
        public bool IsSaved()
        {
            HandleOpenRevertableCollection();
            return _savedActionIndex == _currentStateIndex;
        }

        private void HandleOpenRevertableCollection()
        {
            if (_currentCollection == null)
                return;

            //forcefully end collection
            _collectionNestingLevel = 0;
            EndCollection();

            //throw error to indicate that the developer messed up
            throw new OpenRevertableCollectionException();
        }


        /// <summary>
        /// Stores a number of <see cref="RevertableAction"/>s that count as one action
        /// </summary>
        private sealed class RevertableCollection : RevertableAction
        {
            readonly RevertableAction[] actions;

            /// <summary>
            /// Creates a new <see cref="RevertableCollection"/> form a list of <see cref="RevertableAction"/>s
            /// </summary>
            /// <param name="actions">The <see cref="RevertableAction"/>s that make up this collection, have to be passed in the order they happened!</param>
            public RevertableCollection(RevertableAction[] actions)
            {
                this.actions = actions;
            }

            public override RevertableAction Revert()
            {
                RevertableAction[] newActions = new RevertableAction[actions.Length];

                //emulate undo/redo stack behaivior by starting at the last index of actions and the first index of newActions
                for (int i = 0; i < actions.Length; i++)
                {
                    newActions[i] = actions[actions.Length - 1 - i].Revert();
                }

                return new RevertableCollection(newActions);
            }
        }

        /// <summary>
        /// signalizes to the user/developer that the Action mamger is in an uncertain state since there are some revertable collections left open
        /// </summary>
        private class OpenRevertableCollectionException : Exception
        {
            public override string Message => "Not all revertable collections where closed properly";
        }
    }
}
