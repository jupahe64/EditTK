using EditTK.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Graphics.Scene
{
    public abstract class Scene<TSceneContext> : IObjectHolder, IScene
        where TSceneContext : class
    {
        protected Scene(TSceneContext sceneContext)
        {
            SceneContext = sceneContext;
        }

        protected TSceneContext SceneContext { get; set; }

        public abstract void ForEachObject(ActionPerObject actionPerObject);

        public void Render(GizmoDrawer gizmoDrawer, CommandList cl, ResourceSet viewParamsSet, Pass pass)
        {
            ForEachObject(obj => (obj as ISceneObject<TSceneContext>)?.Draw(SceneContext, gizmoDrawer, cl, viewParamsSet, pass));
        }
    }

    public interface IScene
    {
        void Render(GizmoDrawer gizmoDrawer, CommandList cl, ResourceSet viewParamsSet, Pass pass);
    }
}
