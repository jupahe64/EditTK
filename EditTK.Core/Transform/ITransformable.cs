using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditTK.Core.Transform
{
    public interface ITransformable
    {
        public void ApplyTransformation(Matrix4x4 transformation);
        public void SetTransform(Matrix4x4 transform);
        public Matrix4x4 GetTransform();
    }
}
