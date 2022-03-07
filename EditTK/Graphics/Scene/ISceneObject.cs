using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Graphics.Scene
{
    /// <summary>
    /// An object that's visible in the scene
    /// </summary>
    public interface ISceneObject<TSceneContext>
        where TSceneContext : class
    {
        void Draw(TSceneContext sceneContext, GizmoDrawer gizmoDrawer, CommandList cl, ResourceSet viewParamsSet, Pass pass);
    }
}
