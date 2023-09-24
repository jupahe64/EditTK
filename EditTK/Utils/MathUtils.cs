using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Utils
{
    public class MathUtils
    {
        public static Vector3 ProjectOnPlane(Vector3 point, (Vector3 normal, Vector3 origin) plane)
        {
            return point - plane.normal * Vector3.Dot(point - plane.origin, plane.normal);
        }

        public static Vector3 Orthogonal(Vector3 v)
        {
            //adapted from https://stackoverflow.com/a/11741520
            float x = MathF.Abs(v.X);
            float y = MathF.Abs(v.Y);
            float z = MathF.Abs(v.Z);
            //get most orthogonal basis vector
            Vector3 other = x < y ? (x < z ? Vector3.UnitX : Vector3.UnitZ) : (y < z ? Vector3.UnitY : Vector3.UnitZ);
            return Vector3.Normalize(Vector3.Cross(v, other));
        }

        public static Quaternion GetRotationBetween(Vector3 u, Vector3 v)
        {
            var d = Vector3.Dot(u, v);
            var w = Vector3.Cross(u, v);

            if (d < -0.9999f)
                w = Orthogonal(u);

            return Quaternion.Normalize(new(w, d + MathF.Sqrt(d * d + Vector3.Dot(w, w))));
        }
    }
}
