using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Util
{
    public static class MathUtils3D
    {
        /// <summary>
        /// Calculates the intersetion of a Plane and a Ray
        /// </summary>
        /// <param name="rayVector">The direction vector of the ray, should be normalized</param>
        /// <param name="rayPoint">The origin of the ray, can be any point along the ray</param>
        /// <param name="planeNormal">The normal vector of the plane, should be normalized</param>
        /// <param name="planePoint">The origin of the ray, can be any point on the plane</param>
        /// <returns></returns>
        public static Vector3 IntersectPoint(Vector3 rayVector, Vector3 rayPoint, Vector3 planeNormal, Vector3 planePoint)
        {
            //code from: https://rosettacode.org/wiki/Find_the_intersection_of_a_line_with_a_plane
            var diff = rayPoint - planePoint;
            var prod1 = Vector3.Dot(diff, planeNormal);
            var prod2 = Vector3.Dot(rayVector, planeNormal);
            var prod3 = prod1 / prod2;
            return rayPoint - rayVector * prod3;
        }

        public static (Vector3 axis, double angle) GetSlerpAxisAngle(Matrix4x4 matA, Matrix4x4 matB)
        {
            Vector3[] axesA = new[]
            {
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, matA)),
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, matA)),
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, matA)),
                };

            Vector3[] axesB = new[]
            {
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, matB)),
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, matB)),
                    Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, matB)),
                };

            Vector3 rotationVec;
            int angleMeassureAxis;

            for (int i = 0; i < 3; i++)
            {
                if (axesA[i] == axesB[i])
                {
                    rotationVec = axesA[i];
                    angleMeassureAxis = (i + 1) % 3;
                    goto AXIS_FOUND;
                }

            }

            Vector3[] axesDiff = new[]
            {
                    axesB[0]-axesA[0],
                    axesB[1]-axesA[1],
                    axesB[2]-axesA[2],
                };

            float top1length = 0;
            float top2length = 0;

            int top1index = -1;
            int top2index = -1;

            for (int i = 0; i < 3; i++)
            {
                float length = axesDiff[i].LengthSquared();

                if (length > top1length)
                {
                    top2length = top1length;
                    top2index = top1index;

                    top1index = i;
                    top1length = length;
                }
                else if (length > top2length)
                {
                    top2index = i;
                    top2length = length;
                }
            }

            if (top1length == 0) //rotation is too minimal to meassure it
                return (Vector3.UnitX, 0);

            rotationVec = Vector3.Cross(
                    Vector3.Normalize(axesDiff[top1index]),
                    Vector3.Normalize(axesDiff[top2index])
            );

            //find most orthogonal axis
            float minDotProductProduct = 1;

            angleMeassureAxis = -1;

            for (int i = 0; i < 3; i++)
            {
                if (axesA[i] == -axesB[i])
                    continue;

                float dotProductProduct =
                    Math.Abs(Vector3.Dot(axesA[i], rotationVec)) *
                    Math.Abs(Vector3.Dot(axesB[i], rotationVec));

                if (dotProductProduct < minDotProductProduct)
                {
                    minDotProductProduct = dotProductProduct;
                    angleMeassureAxis = i;
                }
            }


        AXIS_FOUND:
            Vector3 angleMeassureVec = axesA[angleMeassureAxis];
            Vector3 angleMeassureVecDest = axesB[angleMeassureAxis];

            Vector3 rotPlaneVecA = Vector3.Normalize(Vector3.Cross(rotationVec, angleMeassureVec));
            Vector3 rotPlaneVecB = Vector3.Cross(rotationVec, rotPlaneVecA);

            double angle = Math.Atan2(
                Vector3.Dot(rotPlaneVecA, angleMeassureVecDest),
                -Vector3.Dot(rotPlaneVecB, angleMeassureVecDest)
                );

            return (rotationVec, angle);
        }
    }
}
