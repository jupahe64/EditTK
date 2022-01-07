using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditTK.Core3D
{
    public interface ICamera
    {
        Matrix4x4 ViewMatrix { get; }
        Matrix4x4 InverseViewMatrix { get; }

        Vector3 Position { get; }
        Vector3 ForwardVector { get; }
        Vector3 RightVector { get; }
        Vector3 UpVector { get; }

        void Update(float deltaSeconds);
    }
}
