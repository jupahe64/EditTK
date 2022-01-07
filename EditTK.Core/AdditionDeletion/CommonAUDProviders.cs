using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.AdditionDeletion
{
    #region IList
    public static class IListExtensions
    {
        /// <summary>
        /// Removes the given item from the list and g
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool TryRemoveAndGetIndex(this IList list, object item, out int index)
        {
            index = list.IndexOf(item);

            if (index != -1)
            {
                list.RemoveAt(index);
            }

            return index != -1;
        }
    }


    public class ListAUDFunctionProvider : IAUDFunctionProvider<IList, int>
    {
        public void Add(IList list, int index, object item)
        {
            list.Insert(index, item);
        }

        public void Update(IList list, int index, object newValue)
        {
            list[index] = newValue;
        }

        public int Delete(IList list, object obj)
        {
            if(!list.TryRemoveAndGetIndex(obj, out int index))
                throw new Exception($"item {obj} is not in the list");

            return index;
        }
    }

    public class RevertableListAddition : RevertableAddition<IList, int, ListAUDFunctionProvider>
    {
        public RevertableListAddition(MulltiAddInfo[] infos, SingleAddInfo[] singleInfos) : base(infos, singleInfos) { }
    }

    public class RevertableListDeletion : RevertableAddition<IList, int, ListAUDFunctionProvider>
    {
        public RevertableListDeletion(MulltiAddInfo[] infos, SingleAddInfo[] singleInfos) : base(infos, singleInfos) { }
    }
    #endregion

    #region IDictionary
    public class DictAUDFunctionProvider : IAUDFunctionProvider<IDictionary, object>
    {
        public void Add(IDictionary dict, object key, object item)
        {
            dict.Add(key, item);
        }

        public void Update(IDictionary dict, object key, object newValue)
        {
            dict[key] = newValue;
        }

        public object Delete(IDictionary dict, object key)
        {
            object item = dict[key];
            dict.Remove(key);
            return item;
        }
    }

    public class RevertableDictAddition : RevertableAddition<IDictionary, object, DictAUDFunctionProvider>
    {
        public RevertableDictAddition(MulltiAddInfo[] infos, SingleAddInfo[] singleInfos) : base(infos, singleInfos) { }
    }

    public class RevertableDictDeletion : RevertableAddition<IDictionary, object, DictAUDFunctionProvider>
    {
        public RevertableDictDeletion(MulltiAddInfo[] infos, SingleAddInfo[] singleInfos) : base(infos, singleInfos) { }
    }
    #endregion
}
