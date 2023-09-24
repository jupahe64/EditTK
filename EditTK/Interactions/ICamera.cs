using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Interactions
{
    public interface ICamera
    {
        public Quaternion Rotation { get; }
        public Vector3 Position { get; }
    }

    public static class CameraExtensions
    {
        public static Matrix4x4 GetViewProjection(this ICamera cam, float aspectRatio, 
            float fov = (float)((2 * Math.PI) / 5), float nearClipping = 0.01f, float farClipping = 1000f)
        {
            return 
                Matrix4x4.CreateTranslation(-cam.Position) *
                Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(cam.Rotation)) *
                Matrix4x4.CreatePerspectiveFieldOfView(fov,
                    aspectRatio, nearClipping, farClipping);
        }
    }
}
