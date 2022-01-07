using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EditTK.Core.Common;
using System.Collections;

namespace EditTK.Core.Selection
{
    public enum SelectionChangeFunction
    {
        /// <summary>
        /// Selects all given objects
        /// </summary>
        ADD,
        /// <summary>
        /// Deselects all given objects
        /// </summary>
        SUBTRACT,
        /// <summary>
        /// Selects all given objects and deselects all others
        /// </summary>
        REPLACE
    }

    public class SelectionManager
    {
        //TODO do we really need this?
        private readonly IObjectHolder _objectHolder;

        /// <summary>
        /// Creates a new <see cref="SelectionManager"/>
        /// </summary>
        /// 
        /// <param name="objectHolder">
        /// An object holder which holds all objects that should be selectable through this <see cref="SelectionManager"/>. 
        /// 
        /// <para> Note only objects that implement <see cref="ISelectable"/> can be selected </para>
        /// </param>
        public SelectionManager(IObjectHolder objectHolder)
        {
            _objectHolder = objectHolder;
        }

        /// <summary>
        /// Selects a given object or deselects it if it's already selected
        /// </summary>
        /// <param name="objectToSelect">the object to select/deselect, can be an <see cref="ISelectable"/> or <see cref="ISelectableContainer"/></param>
        /// <param name="deselectOther">If set to <see langword="true" /> all other objects will be deselected</param>
        public void ToggleSelect(object objectToSelect, bool deselectOther = true)
        {
            if(objectToSelect is ISelectable selectable)
            {
                bool wasSelected = selectable.Selected;

                if (deselectOther)
                    DeselectAll();

                selectable.Selected = !wasSelected;
            }
            else if (objectToSelect is ISelectableContainer container)
            {
                bool wasDefaultSelection = container.IsDefaultSelection();

                if (deselectOther)
                    DeselectAll();
                else
                    container.DeselectAll();

                if (!wasDefaultSelection)
                    container.SelectDefault();
            }
        }

        /// <summary>
        /// Selects all given objects
        /// </summary>
        /// <param name="objectsToSelect">An <see cref="ISet{}"/> of objects to select</param>
        /// <param name="deselectOther">If set to <see langword="true" /> all other objects will be deselected</param>
        public void Select(ISet<object> objectsToSelect, bool deselectOther = true) => ChangeSelection(objectsToSelect, deselectOther ? SelectionChangeFunction.REPLACE : SelectionChangeFunction.ADD);

        /// <summary>
        /// Deselects all given objects
        /// </summary>
        /// <param name="objectsToDeselect">An <see cref="ISet{}"/> of objects to deselect</param>
        public void Deselect(ISet<object> objectsToDeselect) => ChangeSelection(objectsToDeselect, SelectionChangeFunction.SUBTRACT);

        /// <summary>
        /// <para>Changes the selection given the affected objects and the change function</para>
        /// <para>Use <see cref="Select(ISet{object}, bool)"/> and <see cref="Deselect(ISet{object})"/> for more intuitive usage</para>
        /// </summary>
        /// <param name="objects">the affected objects</param>
        /// <param name="changeFunction">the change function, see 
        /// <see cref="SelectionChangeFunction.ADD"/>, 
        /// <see cref="SelectionChangeFunction.SUBTRACT"/> and 
        /// <see cref="SelectionChangeFunction.REPLACE"/> for further details
        /// </param>
        public void ChangeSelection(ISet<object> objects, SelectionChangeFunction changeFunction)
        {
            if (changeFunction == SelectionChangeFunction.REPLACE)
                DeselectAll();


            bool isSubtract = changeFunction==SelectionChangeFunction.SUBTRACT;

            foreach (var obj in objects)
            {
                if (obj is ISelectable selectable)
                {
                    selectable.Selected = !isSubtract;
                }
                else if (obj is ISelectableContainer container)
                {
                    if (isSubtract)
                    {
                        container.DeselectAll();
                    }
                    else
                    {
                        foreach (var _obj in container.Objects)
                        {
                            if (objects.Contains(obj) || obj is ISelectable s && s.Selected)
                                goto CONTAINER_HANDLED;
                        }
                        container.DeselectAll();
                        container.SelectDefault();
                    }
                }

            CONTAINER_HANDLED:
                continue;
            }
        }

        
        /// <summary>
        /// Selects all objects
        /// </summary>
        public void SelectAll()
        {
            _objectHolder.ForEachObject(SelectAll_PerObject);
        }

        private static void SelectAll_PerObject(object obj)
        {
            if (obj is ISelectable selectable)
                selectable.Selected = true;
            else if (obj is ISelectableContainer container)
                container.SelectAll();
        }

        /// <summary>
        /// Deselects all objects
        /// </summary>
        public void DeselectAll()
        {
            _objectHolder.ForEachObject(DeselectAll_PerObject);
        }

        private static void DeselectAll_PerObject(object obj)
        {
            if (obj is ISelectable selectable)
                selectable.Selected = false;
            else if (obj is ISelectableContainer container)
                container.DeselectAll();
        }

        /// <summary>
        /// Retrieves all selected objects
        /// </summary>
        /// <returns>An <c cref="IEnumerable"/> of all objects that are part of the current selection</returns>
        public void ForEachSelectedObject(Action<object> callBack)
        {
            _objectHolder.ForEachObject((obj) =>
            {
                if (obj is ISelectable selectable)
                {
                    if (selectable.Selected)
                        callBack(obj);
                }
                else if (obj is ISelectableContainer container)
                {
                    foreach (var _obj in container.Objects)
                    {
                        if (_obj is ISelectable _selectable)
                        {
                            if (_selectable.Selected)
                                callBack(obj);
                        }
                    }
                }
            }
            );
            
        }
    }

    /// <summary>
    /// Represents an object that can be selected
    /// </summary>
    public interface ISelectable
    {
        bool Selected { get; set; }
    }

    /// <summary>
    /// Represents a container of selectable objects, with a default selection
    /// </summary>
    public interface ISelectableContainer
    {
        IEnumerable Objects { get; }

        IEnumerable GetDefaultSelection();
    }

    /// <summary>
    /// Provides the functionality for <see cref="ISelectableContainer"/>
    /// </summary>
    public static class SelectableContainer
    {
        /// <summary>
        /// Selects all objects inside this container
        /// </summary>
        public static void SelectAll(this ISelectableContainer container)
        {
            foreach (var obj in container.Objects)
            {
                if (obj is ISelectable selectable)
                    selectable.Selected = true;
            }
        }

        /// <summary>
        /// Deselects all objects inside this container
        /// </summary>
        public static void DeselectAll(this ISelectableContainer container)
        {
            foreach (var obj in container.Objects)
            {
                if (obj is ISelectable selectable)
                    selectable.Selected = false;
            }
        }

        /// <summary>
        /// Selects the default selection of this container
        /// </summary>
        public static void SelectDefault(this ISelectableContainer container)
        {
            container.DeselectAll();
            foreach (var obj in container.GetDefaultSelection())
            {
                if (obj is ISelectable selectable)
                    selectable.Selected = true;
            }
        }

        /// <summary>
        /// Checks if the current selection inside this container matches it's default selection
        /// </summary>
        /// <returns><see langword="true" /> if it matches 100%, otherwise <see langword="false" /></returns>
        public static bool IsDefaultSelection(this ISelectableContainer container)
        {
            int defaultSelectionCount = 0;

            foreach (var obj in container.GetDefaultSelection())
            {
                if (!(obj is ISelectable selectable && selectable.Selected))
                    return false; //expected to be selected but wasn't
                
                defaultSelectionCount++;
            }

            int selectionCount = 0;

            foreach (var obj in container.Objects)
            {
                if (obj is ISelectable selectable && selectable.Selected)
                    selectionCount++;

                if (selectionCount > defaultSelectionCount)
                    return false; //more objects where selected than expected
            }

            return true; //no mismatch found
        }

        /// <summary>
        /// Checks if any object inside this container is selected
        /// </summary>
        /// <returns><see langword="true" /> if a selected object was found, otherwise <see langword="false" /></returns>
        public static bool IsAnythingSelected(this ISelectableContainer container)
        {
            foreach (var obj in container.Objects)
            {
                if (obj is ISelectable s && s.Selected)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Provides helper functions for dealing with selectable objects
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// Checks if a given object is selected
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsSelected(object obj)
        {
            if (obj is ISelectable selectable)
                return selectable.Selected;
            else if (obj is ISelectableContainer container)
                return container.IsAnythingSelected();
            else
                return false;
        }
    }
}
