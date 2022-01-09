using EditTK.Graphics.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Graphics
{
    /// <summary>
    /// Helpful utilities for dealing with Veldrid
    /// </summary>
    public static class VeldridUtils
    {
        /// <summary>
        /// A vertex with a 3d position and a texture coordinate
        /// </summary>
        public struct VertexPositionTexture
        {
            [VertexAttributeAtrribute("pos", VertexElementFormat.Float3)]
            public Vector3 Posisiton;

            [VertexAttributeAtrribute("uv", VertexElementFormat.Float2)]
            public Vector2 UV;

            public VertexPositionTexture(Vector3 pos, Vector2 uv)
            {
                Posisiton = pos;
                UV = uv;
            }
        }

        /// <summary>
        /// A vertex with a 3d position and a vertex color
        /// </summary>
        public struct VertexPositionColor
        {
            [VertexAttributeAtrribute("pos", VertexElementFormat.Float3)]
            public Vector3 Posisiton;

            [VertexAttributeAtrribute("col", VertexElementFormat.Float4)]
            public Vector4 Color;

            public VertexPositionColor(Vector3 pos, Vector4 col)
            {
                Posisiton = pos;
                Color = col;
            }
        }

        /// <summary>
        /// A vertex with a 2d position and a texture coordinate
        /// </summary>
        public struct VertexFullscreenQuad
        {
            [VertexAttributeAtrribute("pos", VertexElementFormat.Float2)]
            public Vector2 Posisiton;

            [VertexAttributeAtrribute("uv", VertexElementFormat.Float2)]
            public Vector2 UV;

            public VertexFullscreenQuad(Vector2 pos, Vector2 uv)
            {
                Posisiton = pos;
                UV = uv;
            }
        }

        public static Matrix4x4 CreatePerspective(
            GraphicsDevice gd,
            bool useReverseDepth,
            float fov,
            float aspectRatio,
            float near, float far)
        {
            Matrix4x4 persp;
            if (useReverseDepth)
            {
                persp = CreatePerspective(fov, aspectRatio, far, near);
            }
            else
            {
                persp = CreatePerspective(fov, aspectRatio, near, far);
            }
            if (gd.IsClipSpaceYInverted)
            {
                persp *= new Matrix4x4(
                    1, 0, 0, 0,
                    0, -1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
            }

            return persp;
        }

        private static Matrix4x4 CreatePerspective(float fov, float aspectRatio, float near, float far)
        {
            if (fov <= 0.0f || fov >= MathF.PI)
                throw new ArgumentOutOfRangeException(nameof(fov));

            if (near <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(near));

            if (far <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(far));

            float yScale = 1.0f / MathF.Tan(fov * 0.5f);
            float xScale = yScale / aspectRatio;

            Matrix4x4 result;

            result.M11 = xScale;
            result.M12 = result.M13 = result.M14 = 0.0f;

            result.M22 = yScale;
            result.M21 = result.M23 = result.M24 = 0.0f;

            result.M31 = result.M32 = 0.0f;
            var negFarRange = float.IsPositiveInfinity(far) ? -1.0f : far / (near - far);
            result.M33 = negFarRange;
            result.M34 = -1.0f;

            result.M41 = result.M42 = result.M44 = 0.0f;
            result.M43 = near * negFarRange;

            return result;
        }

        public static Matrix4x4 CreateOrtho(
            GraphicsDevice gd,
            bool useReverseDepth,
            float left, float right,
            float bottom, float top,
            float near, float far)
        {
            Matrix4x4 ortho;
            if (useReverseDepth)
            {
                ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, far, near);
            }
            else
            {
                ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, near, far);
            }
            if (gd.IsClipSpaceYInverted)
            {
                ortho *= new Matrix4x4(
                    1, 0, 0, 0,
                    0, -1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
            }

            return ortho;
        }


        /// <summary>
        /// Gets all vertices necessary for a full-screen-quad, setup specificly for the gived <see cref="GraphicsDevice"/>
        /// </summary>
        public static VertexFullscreenQuad[] GetFullScreenQuadVerts(GraphicsDevice gd)
        {
            float top, bottom;

            if (gd.IsUvOriginTopLeft)
            {
                top = 0;
                bottom = 1;
            }
            else
            {
                top = 1;
                bottom = 0;
            }

            if (gd.IsClipSpaceYInverted)
            {
                return new VertexFullscreenQuad[]
                {
                        new (new Vector2(-1, -1), new Vector2(0, top)),
                        new (new Vector2( 1, -1), new Vector2(1, top)),
                        new (new Vector2( 1,  1), new Vector2(1, bottom)),
                        new (new Vector2(-1,  1), new Vector2(0, bottom))
                };
            }
            else
            {
                return new VertexFullscreenQuad[]
                {
                        new (new Vector2(-1,  1), new Vector2(0, top)),
                        new (new Vector2( 1,  1), new Vector2(1, top)),
                        new (new Vector2( 1, -1), new Vector2(1, bottom)),
                        new (new Vector2(-1, -1), new Vector2(0, bottom))
                };
            }
        }

        /// <summary>
        /// Gets all vertices necessary for a full-screen-quad that has a single [copy]uv coordinate, setup specificly for the gived <see cref="GraphicsDevice"/>
        /// </summary>
        public static VertexFullscreenQuad[] GetFullScreenCopyQuadVerts(GraphicsDevice gd, Vector2 copyUV)
        {
            if (gd.IsClipSpaceYInverted)
            {
                return new VertexFullscreenQuad[]
                {
                        new (new Vector2(-1, -1), copyUV),
                        new (new Vector2( 1, -1), copyUV),
                        new (new Vector2( 1,  1), copyUV),
                        new (new Vector2(-1,  1), copyUV)
                };
            }
            else
            {
                return new VertexFullscreenQuad[]
                {
                        new (new Vector2(-1,  1), copyUV),
                        new (new Vector2( 1,  1), copyUV),
                        new (new Vector2( 1, -1), copyUV),
                        new (new Vector2(-1, -1), copyUV)
                };
            }
        }

        /// <summary>
        /// All indices necessary to render a quad from 4 vertices
        /// </summary>
        public static readonly ushort[] QuadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

        /// <summary>
        /// Calculates the number of bytes a given array will take up in memory
        /// </summary>
        public static unsafe uint GetSizeInBytes<T>(T[] data) where T : unmanaged
        {
            return (uint)(sizeof(T) * data.Length);
        }

        /// <summary>
        /// The number of bytes a <see cref="Matrix4x4"/> takes up
        /// </summary>
        public const int SIZE_OF_MAT4 = sizeof(float) * 4 * 4;
        /// <summary>
        /// The number of bytes a <see cref="Vector4"/> takes up
        /// </summary>
        public const int SIZE_OF_VEC4 = sizeof(float) * 4;
        /// <summary>
        /// The number of bytes a <see cref="Vector2"/> takes up
        /// </summary>
        public const int SIZE_OF_VEC2 = sizeof(float) * 2;

        /// <summary>
        /// The minimum size of a buffer to contain the given amount of <paramref name="neededBytes"/>
        /// </summary>
        public static uint GetMinimumBufferSize(uint neededBytes) => (uint)(Math.Ceiling(neededBytes / 16.0) * 16);
        /// <summary>
        /// The minimum size of a buffer to contain the given amount of <paramref name="neededBytes"/>
        /// </summary>
        public static uint GetMinimumBufferSize(int neededBytes)  => (uint)(Math.Ceiling(neededBytes / 16.0) * 16);

        /// <summary>
        /// Creates a Uniformbuffer and initializes it with the given data
        /// </summary>
        public static unsafe DeviceBuffer CreateUniformBuffer<T>(T data) where T : unmanaged
        {
            Debug.Assert(GraphicsAPI.ResourceFactory != null);
            Debug.Assert(GraphicsAPI.GD != null);

            var uniformBuffer = GraphicsAPI.ResourceFactory.CreateBuffer(new BufferDescription(GetMinimumBufferSize(sizeof(T)), BufferUsage.UniformBuffer));

            GraphicsAPI.GD.UpdateBuffer(uniformBuffer, 0, data);

            return uniformBuffer;
        }
    }
}
