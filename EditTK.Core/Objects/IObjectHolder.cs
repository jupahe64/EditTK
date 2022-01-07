using System;
using System.Collections;
using System.Text;

namespace EditTK.Core.Common
{
    /// <summary>
    /// An interface for any class that keeps track of objects
    /// </summary>
    public interface IObjectHolder
    {
        /// <summary>
        /// Calls the <paramref name="callBack"/> for each object in this <see cref="IObjectHolder"/>
        /// </summary>
        void ForEachObject(Action<object> callBack);
    }
}
