using EditTK.Core.UndoRedo;
using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.AdditionDeletion
{
    public class RevertableDeletion<TCollection, TKey, TFunctionProvider> : RevertableAction where TFunctionProvider : IAUDFunctionProvider<TCollection, TKey>, new()
    {

        private readonly DeleteInListInfo[] _infos;
        private readonly SingleDeleteInListInfo[] _singleInfos;


        public RevertableDeletion(DeleteInListInfo[] infos, SingleDeleteInListInfo[] singleInfos)
        {
            _infos = infos;
            _singleInfos = singleInfos;
        }

        public override RevertableAction Revert() //Insert all deleted objects back in
        {
            TFunctionProvider functionProvider = new TFunctionProvider();

            TCollection[] lists = new TCollection[_infos.Length + _singleInfos.Length];
            int i_lists = 0;


            //Revert Lists
            RevertableAddition<TCollection, TKey, TFunctionProvider>.MulltiAddInfo[] addInfos = new RevertableAddition<TCollection, TKey, TFunctionProvider>.MulltiAddInfo[_infos.Length];
            int i_addInfos = 0;

            foreach (DeleteInListInfo info in _infos)
            {
                addInfos[i_addInfos] = new RevertableAddition<TCollection, TKey, TFunctionProvider>.MulltiAddInfo(new object[info.infos.Length], info.list);
                int i_info = 0;
                for (int i = info.infos.Length - 1; i >= 0; i--) //loop through backwards so the indices aren't messed up
                {
                    addInfos[i_addInfos].objs[i_info++] = info.infos[i].obj;
                    functionProvider.Add(info.list, info.infos[i].key, info.infos[i].obj);
                }
                lists[i_lists++] = info.list;
                i_addInfos++;
            }

            //Revert Single Deletions
            var addSingleInfos = new RevertableAddition<TCollection, TKey, TFunctionProvider>.SingleAddInfo[_singleInfos.Length];
            i_addInfos = 0;

            foreach (SingleDeleteInListInfo info in _singleInfos)
            {
                addSingleInfos[i_addInfos++] = new RevertableAddition<TCollection, TKey, TFunctionProvider>.SingleAddInfo(info.obj, info.list);
                functionProvider.Add(info.list, info.index, info.obj);
                lists[i_lists++] = info.list;
            }

            return new RevertableAddition<TCollection, TKey, TFunctionProvider>(addInfos, addSingleInfos);
        }

        public struct DeleteInfo
        {
            public DeleteInfo(object obj, TKey key)
            {
                this.obj = obj;
                this.key = key;
            }
            public object obj;
            public TKey key;
        }

        public struct DeleteInListInfo
        {
            public DeleteInListInfo(DeleteInfo[] infos, TCollection list)
            {
                this.infos = infos;
                this.list = list;
            }
            public DeleteInfo[] infos;
            public TCollection list;
        }

        public struct SingleDeleteInListInfo
        {
            public SingleDeleteInListInfo(object obj, TKey key, TCollection list)
            {
                this.obj = obj;
                this.index = key;
                this.list = list;
            }
            public object obj;
            public TKey index;
            public TCollection list;
        }
    }
}
