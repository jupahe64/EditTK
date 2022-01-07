using EditTK.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Cameras
{
    /// <summary>
    /// Provides the base of a Camera for 3d rendering
    /// </summary>
    public abstract class Camera
    {
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 InverseViewMatrix { get; private set; }
        public Matrix4x4 RotationMatrix { get; private set; }

        public Vector3 Position { get; private set; }
        public Vector3 ForwardVector { get; private set; }
        public Vector3 RightVector { get; private set; }
        public Vector3 UpVector { get; private set; }

        public abstract void Update(float deltaSeconds);

        protected void UpdateViewMatrix(Matrix4x4 viewMatrix)
        {
            ViewMatrix = viewMatrix;


            Debug.Assert(Math.Abs(viewMatrix.Row0().Xyz().LengthSquared()-1)<0.1f, "RotationMatrix is not unit length");
            Debug.Assert(Math.Abs(viewMatrix.Row1().Xyz().LengthSquared()-1)<0.1f, "RotationMatrix is not unit length");
            Debug.Assert(Math.Abs(viewMatrix.Row2().Xyz().LengthSquared()-1)<0.1f, "RotationMatrix is not unit length");

            if (!Matrix4x4.Invert(viewMatrix, out Matrix4x4 inverse))
                throw new ArgumentException("The given matrix is not invertable");

            InverseViewMatrix = inverse;

            Position = inverse.Translation;

            RightVector   =  inverse.Row0().Xyz();
            UpVector      =  inverse.Row1().Xyz();
            ForwardVector = -inverse.Row2().Xyz();

            var rotMtx = viewMatrix;
            rotMtx.Translation = Vector3.Zero;

            RotationMatrix = rotMtx;
        }

        protected void UpdateProjectionMatrix(Matrix4x4 projectionMatrix)
        {
            ProjectionMatrix = projectionMatrix;
        }
    }
}
