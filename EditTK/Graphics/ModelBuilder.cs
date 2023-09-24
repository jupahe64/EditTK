using Silk.NET.WebGPU.Safe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Graphics
{
    /// <summary>
    /// Helps with the construction of a <see cref="RenderableMesh"/>
    /// by providing simple to use methods for adding primitives
    /// and generating the needed vertex and index buffers behind the scenes
    /// <para>The resulting <see cref="RenderableMesh"/> will use <see langword="uint"/> as it's index format</para>
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class ModelBuilder<TIndex, TVertex>
        where TVertex : unmanaged
        where TIndex : unmanaged, IUnsignedNumber<TIndex>
    {
        private readonly List<TVertex> _vertices = new();
        private readonly Dictionary<TVertex, TIndex> _vertexLookup = new();
        private readonly List<TIndex> _indices = new();

        private void AddVertex(TVertex vertex)
        {
            if (_vertexLookup.TryGetValue(vertex, out var index))
            {
                _indices.Add(index);
            }
            else
            {
                TIndex last = TIndex.CreateChecked(_vertices.Count);
                _indices.Add(last);
                _vertices.Add(vertex);
                _vertexLookup.Add(vertex, last);
            }
        }

        public ModelBuilder<TIndex, TVertex> AddTriangle(TVertex v1, TVertex v2, TVertex v3)
        {
            AddVertex(v1);
            AddVertex(v2);
            AddVertex(v3);

            return this;
        }

        public ModelBuilder<TIndex, TVertex> AddPlane(TVertex v1, TVertex v2, TVertex v3, TVertex v4)
        {
            AddTriangle(v2, v1, v3);
            AddTriangle(v2, v3, v4);

            return this;
        }

        public RenderableMesh GetModel(DevicePtr device)
        {
            return RenderableMesh.Create<TIndex, TVertex>(device,
                CollectionsMarshal.AsSpan(_indices),
                CollectionsMarshal.AsSpan(_vertices)
            );
        }

        public void GetData(out ReadOnlySpan<TIndex> indices, out ReadOnlySpan<TVertex> vertices)
        {
            indices = CollectionsMarshal.AsSpan(_indices);
            vertices = CollectionsMarshal.AsSpan(_vertices);
        }
    }
}
