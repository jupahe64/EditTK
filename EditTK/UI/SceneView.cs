using EditTK.Core3D.Common;
using EditTK.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.UI
{
    public abstract class SceneView
    {
        public IScene? Scene { get; set; }
        public Vector2 Size { get; set; }
        public Camera Camera { get; set; }

        private readonly GizmoDrawer _gizmoDrawer;

        public SceneView(Vector2 size, Camera camera)
        {
            _gizmoDrawer = new GizmoDrawer(size, camera);
            Size = size;
            Camera = camera;
        }

        public void Draw(CommandList cl)
        {
            //Scene.Render(_gizmoDrawer, cl, )
        }
    }
}
