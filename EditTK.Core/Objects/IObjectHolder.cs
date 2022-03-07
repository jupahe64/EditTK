using System;
using System.Collections;
using System.Text;

namespace EditTK.Core.Common
{
    public delegate void ActionPerObject(object obj);

    /// <summary>
    /// An interface for any class that keeps track of objects
    /// </summary>
    public interface IObjectHolder
    {
        /// <summary>
        /// Calls the <paramref name="actionPerObject"/> for each object in this <see cref="IObjectHolder"/>
        /// </summary>
        void ForEachObject(ActionPerObject actionPerObject);
    }
}
