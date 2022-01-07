using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.AdditionDeletion
{
    public class AdditionManager
    {

        private readonly Dictionary<IList, ListInfo> _infosByLists = new Dictionary<IList, ListInfo>();


        public void Add(IList list, params AddInfo[] infos)
        {
            if (!_infosByLists.ContainsKey(list))
                _infosByLists[list] = new ListInfo(new List<AddInfo>(), list.Count);

            _infosByLists[list].infos.AddRange(infos);
            _infosByLists[list].estimatedLength += infos.Length;
        }

        public void Add(IList list, params object[] objects)
        {
            if (!_infosByLists.ContainsKey(list))
                _infosByLists[list] = new ListInfo(new List<AddInfo>(), list.Count);

            AddInfo[] infos = new AddInfo[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                infos[i] = new AddInfo(objects[i], _infosByLists[list].estimatedLength + i);
            }

            _infosByLists[list].infos.AddRange(infos);
            _infosByLists[list].estimatedLength += infos.Length;
        }

        public struct AddInfo
        {
            public object obj;
            public int index;

            public AddInfo(object obj, int index)
            {
                this.obj = obj;
                this.index = index;
            }
        }

        public class ListInfo
        {
            public List<AddInfo> infos;
            public int estimatedLength;

            public ListInfo(List<AddInfo> infos, int estimatedLength)
            {
                this.infos = infos;
                this.estimatedLength = estimatedLength;
            }
        }

        public void Execute(UndoRedo.ActionManager actionManager)
        {
            if (_infosByLists.Count == 0)
                return;

            List<RevertableListAddition.MulltiAddInfo> infos = new List<RevertableListAddition.MulltiAddInfo>();
            List<RevertableListAddition.SingleAddInfo> singleInfos = new List<RevertableListAddition.SingleAddInfo>();

            
            foreach (KeyValuePair<IList, ListInfo> pair in _infosByLists)
            {
                var list = pair.Key;
                var collectedInfos = pair.Value.infos;

                if (collectedInfos.Count == 0)
                    throw new Exception("entry has no objects");

                if (collectedInfos.Count == 1)
                {
                    singleInfos.Add(new RevertableListAddition.SingleAddInfo(collectedInfos[0].obj, list));
                    list.Add(collectedInfos[0].obj);
                }
                else
                {
                    object[] objs = new object[collectedInfos.Count];
                    int i = 0;
                    foreach (AddInfo info in collectedInfos)
                    {
                        objs[i++] = info.obj;
                        list.Add(info.obj);
                    }
                    infos.Add(new RevertableListAddition.MulltiAddInfo(objs, list));
                }
            }

            actionManager.SubmitAction(new RevertableListAddition(infos.ToArray(), singleInfos.ToArray()));
        }
    }
}
