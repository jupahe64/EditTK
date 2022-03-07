using System.Collections.Generic;

namespace EditTK.Graphics.Common
{
    /// <summary>
    /// Helps with the construction of a <see cref="GenericModel{TIndex, TVertex}"/>
    /// by providing simple to use methods for adding primitives
    /// and generating the needed vertex and index buffers behind the scenes
    /// <para>The resulting <see cref="GenericModel{TIndex, TVertex}"/> will use <see langword="int"/> as it's index format</para>
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class GenericModelBuilder<TVertex>
        where TVertex : unmanaged
    {
        private readonly List<TVertex> _vertices = new();
        private readonly Dictionary<TVertex, int> _vertexLookup = new();
        private readonly List<int> _indices = new();

        private void AddVertex(TVertex vertex)
        {
            if(_vertexLookup.TryGetValue(vertex, out var index))
            {
                _indices.Add(index);
            }
            else
            {
                int last = _vertices.Count;
                _indices.Add(last);
                _vertices.Add(vertex);
                _vertexLookup.Add(vertex, last);
            }
        }

        public void AddTriangle(TVertex v1, TVertex v2, TVertex v3)
        {
            AddVertex(v1);
            AddVertex(v2);
            AddVertex(v3);
        }

        public void AddPlane(TVertex v1, TVertex v2, TVertex v3, TVertex v4)
        {
            AddTriangle(v1, v2, v3);
            AddTriangle(v2, v4, v3);
        }

        public GenericModel<int, TVertex> GetModel()
        {
            return new GenericModel<int, TVertex>(
                _vertices.ToArray(),
                _indices.ToArray()
                );
        }
    }
}