using EditTK.Core.UndoRedo;
using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.AdditionDeletion
{
    public class RevertableAddition<TCollection,TKey, TFunctionProvider> : RevertableAction where TFunctionProvider : IAUDFunctionProvider<TCollection, TKey>, new()
    {

        private readonly MulltiAddInfo[] _infos;
        private readonly SingleAddInfo[] _singleInfos;


        public RevertableAddition(MulltiAddInfo[] infos, SingleAddInfo[] singleInfos)
        {
            _infos = infos;
            _singleInfos = singleInfos;
        }

        public override RevertableAction Revert() //Remove added objects from their lists
        {
            TFunctionProvider functionProvider = new TFunctionProvider();

            TCollection[] lists = new TCollection[_infos.Length + _singleInfos.Length];
            int i_lists = 0;


            //Revert Lists
            var deleteInfos = new RevertableDeletion<TCollection, TKey, TFunctionProvider>.DeleteInListInfo[_infos.Length];
            int i_deleteInfos = 0;

            foreach (MulltiAddInfo info in _infos)
            {
                deleteInfos[i_deleteInfos] = new RevertableDeletion<TCollection, TKey, TFunctionProvider>.DeleteInListInfo(new RevertableDeletion<TCollection, TKey, TFunctionProvider>.DeleteInfo[info.objs.Length], info.collection);
                int i_info = 0;
                for (int i = 0; i < info.objs.Length; i++)
                {
                    deleteInfos[i_deleteInfos].infos[i_info++] = new RevertableDeletion<TCollection, TKey, TFunctionProvider>.DeleteInfo(
                        info.objs[i],
                        functionProvider.Delete(info.collection, info.objs[i])
                    );
                }
                lists[i_lists++] = info.collection;
                i_deleteInfos++;
            }

            //Revert Single Additions
            var deleteSingleInfos = new RevertableDeletion<TCollection, TKey, TFunctionProvider>.SingleDeleteInListInfo[_singleInfos.Length];
            i_deleteInfos = 0;

            foreach (SingleAddInfo info in _singleInfos)
            {
                deleteSingleInfos[i_deleteInfos++] = new RevertableDeletion<TCollection, TKey, TFunctionProvider>.SingleDeleteInListInfo(
                    info.obj,
                    functionProvider.Delete(info.collection, info.obj), 
                    info.collection);
                
                lists[i_lists++] = info.collection;
            }

            return new RevertableDeletion<TCollection, TKey, TFunctionProvider>(deleteInfos, deleteSingleInfos);
        }

        public struct MulltiAddInfo
        {
            public MulltiAddInfo(object[] objs, TCollection collection)
            {
                this.objs = objs;
                this.collection = collection;
            }
            public object[] objs;
            public TCollection collection;
        }

        public struct SingleAddInfo
        {
            public SingleAddInfo(object obj, TCollection collection)
            {
                this.obj = obj;
                this.collection = collection;
            }
            public object obj;
            public TCollection collection;
        }
    }
}
