using System.Numerics;
using System.Runtime.CompilerServices;

namespace EditTK.Core3D.Util
{
    public static class NumericsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Row0(this Matrix4x4 self) => new Vector4(self.M11, self.M12, self.M13, self.M14);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Row1(this Matrix4x4 self) => new Vector4(self.M21, self.M22, self.M23, self.M24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Row2(this Matrix4x4 self) => new Vector4(self.M31, self.M32, self.M33, self.M34);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Row3(this Matrix4x4 self) => new Vector4(self.M41, self.M42, self.M43, self.M44);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Xyz(this Vector4 self) => new Vector3(self.X, self.Y, self.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Xy(this Vector4 self) => new Vector2(self.X, self.Y);
    }
}
