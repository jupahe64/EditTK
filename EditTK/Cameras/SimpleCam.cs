using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Cameras
{
    /// <summary>
    /// A bare bones implementation of a <see cref="Camera"/>
    /// </summary>
    public class SimpleCam : Camera
    {
        public override void Update(float deltaSeconds)
        {
            
        }

        public void SetViewMatrix(Matrix4x4 viewMatrix)
        {
            UpdateViewMatrix(viewMatrix);
        }

        public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
        {
            UpdateProjectionMatrix(projectionMatrix);
        }
    }
}
