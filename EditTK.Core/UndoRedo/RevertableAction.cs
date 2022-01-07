using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.UndoRedo
{
    /// <summary>
    /// Captures all informations needed to Revert a specific action
    /// </summary>
    public abstract class RevertableAction
    {
        /// <summary>
        /// Reverts/undoes this action, returning everything to the previous state
        /// </summary>
        /// <returns>An action that captures all changes of the Revert, it can be used for redo</returns>
        public abstract RevertableAction Revert();

        /// <summary>
        /// Can be used to store various information about this Action like a summary text or a picture
        /// </summary>
        public object? Tag { get; set; }
    }
}
