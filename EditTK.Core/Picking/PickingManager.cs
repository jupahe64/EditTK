using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EditTK.Core.Picking
{
    /// <summary>
    /// Manages object ID distribution and object picking by ID. 
    /// 
    /// <para>
    /// Only use this if the object picking happens on the GPU
    /// or if for some other reason you can only provide the object reference as an int.
    /// </para>
    /// </summary>
    public sealed class PickingManager
    {
        object[]? _pickableObjects;

        public PickableObjectsRebuilder GetRebuilder() => new PickableObjectsRebuilder();

        /// <summary>
        /// Rebuilds the internal list of pickable objects from a <see cref="PickableObjectsRebuilder"/>
        /// </summary>
        /// <param name="rebuilder">A <see cref="PickableObjectsRebuilder"/> that every pickable object was submitted to</param>
        public void Rebuild(PickableObjectsRebuilder rebuilder)
        {
            _pickableObjects = rebuilder.PickableObjects.ToArray();
        }

        /// <summary>
        /// Gets a pickable object by it's ID
        /// </summary>
        /// <param name="pickingID">The picking ID of the pickable object</param>
        /// <returns>The object with the given ID</returns>
        public object GetObject(int pickingID)
        {
            Debug.Assert(_pickableObjects != null);

            if (pickingID < 0 || pickingID >= _pickableObjects.Length)
                throw new IndexOutOfRangeException($"The ID {pickingID} is outside of the range of pickable object IDs");

            return _pickableObjects[pickingID];
        }
    }

    /// <summary>
    /// Manages the rebuilding process of a <see cref="PickingManager"/>'s pickable objects. You can submit a pickable object by calling <see cref="Submit"/>
    /// </summary>
    public sealed class PickableObjectsRebuilder
    {
        internal List<object> PickableObjects { get; private set; } = new List<object>();

        internal PickableObjectsRebuilder()
        {

        }

        /// <summary>
        /// Submits an object to the list of pickable objects
        /// </summary>
        /// <param name="obj">The object to submit</param>
        /// <returns>A unique pickingID that this object can be accessed/picked by</returns>
        public int Submit(object obj)
        {
            PickableObjects.Add(obj);

            return PickableObjects.Count - 1;
        }
    }
}
