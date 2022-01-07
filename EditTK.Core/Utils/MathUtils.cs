using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace EditTK.Core.Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Conversion factor from radians to degrees
        /// </summary>
        public const double RADIANS_TO_DEGREES = 180.0 / Math.PI;

        /// <summary>
        /// Conversion factor from degrees to radians
        /// </summary>
        public const double DEGREES_TO_RADIANS = Math.PI / 180.0;

        /// <summary>
        /// Interpolates between 2 values based on a factor t
        /// </summary>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        /// <param name="t">The mix vector</param>
        /// <returns>The interpolated value</returns>
        public static double Mix(double a, double b, double t) => a * (1 - t) + b * t;

        /// <summary>
        /// Provides the common implementation of <see cref="GetNewRotationAngle"/> and <see cref="GetNewUnboundRotationDegrees"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ExtractRotation(double oldUnboundRotation, double newBoundRotation, double FULL_ROTATION, double HALF_ROTATION)
        {
            double oldR = (oldUnboundRotation % FULL_ROTATION + FULL_ROTATION) % FULL_ROTATION;

            double newR = (newBoundRotation % FULL_ROTATION + FULL_ROTATION) % FULL_ROTATION;

            double delta = newR - oldR;

            double abs = Math.Abs(delta);

            double sign = Math.Sign(delta);

            if (abs > HALF_ROTATION)
                return -(FULL_ROTATION - abs) * sign;
            else
                return delta;
        }

        /// <summary>
        /// Gets the rotation with the smallest angle between two angles in the range of 0 to TAU
        /// </summary>
        /// <param name="angleA">An angle in radians</param>
        /// <param name="angleB">An angle in radians</param>
        /// <returns>A positive angle if the rotation is clockwise and a negative angle if the rotation is counter clockwise (in radians)</returns>
        public static double GetShortestRadRotationBetween(double angleA, double angleB) =>
            ExtractRotation(angleA, angleB, Math.PI * 2, Math.PI);

        /// <summary>
        /// Gets the rotation with the smallest angle between two angles in the range of 0 to 360
        /// </summary>
        /// <param name="angleA">An angle in degrees</param>
        /// <param name="angleB">An angle in degrees</param>
        /// <returns>A positive angle if the rotation is clockwise and a negative angle if the rotation is counter clockwise (in degrees)</returns>
        public static double GetShortestDegRotationBetween(double angleA, double angleB) =>
            ExtractRotation(angleA, angleB, 360, 180);

        /// <summary>
        /// Calculates the new rotation angle based on the old rotation angle and a vector representing the new rotation
        /// </summary>
        /// <param name="oldAngle"></param>
        /// <param name="newRotationAngleVec"></param>
        /// <returns></returns>
        public static double GetNewRotationAngle(double oldAngle, Vector2 newRotationAngleVec) =>
            oldAngle + GetShortestRadRotationBetween(oldAngle, Math.Atan2(newRotationAngleVec.Y, newRotationAngleVec.X));


        /// <summary>
        /// Rounds a value to the nearest mutliple of a given unit
        /// </summary>
        public static double Round(double value, double unit = 1) => Math.Round(value / unit) * unit;

        /// <summary>
        /// Rounds a value to the nearest mutliple of a given unit
        /// </summary>
        public static decimal Round(decimal value, decimal unit = 1) => Math.Round(value / unit) * unit;

        /// <summary>
        /// Rotates a given <see cref="Vector2"/> by 90 degrees to the right
        /// </summary>
        /// <param name="vec">The vector to be rotated</param>
        /// <returns>The rotated vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateRight(Vector2 vec) => new Vector2(vec.Y, -vec.X);


        //from https://stackoverflow.com/a/9755252 with slight modifications

        /// <summary>
        /// Checks if a given point is inside a Triangle made of the given points
        /// </summary>
        /// <param name="p">The point to check</param>
        /// <param name="a">Point A of the triangle</param>
        /// <param name="b">Point B of the triangle</param>
        /// <param name="c">Point C of the triangle</param>
        /// <returns> <see langword="true"/> if the point lies inside the triangle, <see langword="false"/> otherwise</returns>
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float AP_x = p.X - a.X;
            float AP_y = p.Y - a.Y;

            float CP_x = p.X - b.X;
            float CP_y = p.Y - b.Y;

            bool  s_ab =   (b.X - a.X) * AP_y - (b.Y - a.Y) * AP_x > 0.0;
                           
            if (/*s_ac*/   (c.X - a.X) * AP_y - (c.Y - a.Y) * AP_x > 0.0 == s_ab) return false;
                           
            if (/*s_cb*/   (c.X - b.X) * CP_y - (c.Y - b.Y) * CP_x > 0.0 != s_ab) return false;

            return true;
        }


        /// <summary>
        /// Checks if a given point is inside a Quad made of the given points
        /// </summary>
        /// <param name="p">The point to check</param>
        /// <param name="a">Point A of the quad</param>
        /// <param name="b">Point B of the quad</param>
        /// <param name="c">Point C of the quad</param>
        /// <param name="d">Point D of the quad</param>
        /// <returns> <see langword="true"/> if the point lies inside the quad, <see langword="false"/> otherwise</returns>
        public static bool IsPointInQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float AP_x = p.X - a.X;
            float AP_y = p.Y - a.Y;

            float CP_x = p.X - c.X;
            float CP_y = p.Y - c.Y;

            bool  s_ab =   (b.X - a.X) * AP_y - (b.Y - a.Y) * AP_x > 0.0;
                           
            if (/*s_ad*/   (d.X - a.X) * AP_y - (d.Y - a.Y) * AP_x > 0.0 == s_ab) return false;
                           
            if (/*s_cb*/   (b.X - c.X) * CP_y - (b.Y - c.Y) * CP_x > 0.0 == s_ab) return false;
                           
            if (/*s_cd*/   (d.X - c.X) * CP_y - (d.Y - c.Y) * CP_x > 0.0 != s_ab) return false;

            return true;
        }
    }
}
