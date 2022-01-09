using System;
using Veldrid;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// A model that can be drawn by a <see cref="GenericModelRenderer{TIndex, TVertex}"/>
    /// </summary>
    /// <typeparam name="TIndex">The index type used in the IndexBuffer</typeparam>
    /// <typeparam name="TVertex">The vertex type used in the VertexBuffer
    ///                     <para>Note: All fields need to have a <see cref="VertexAttributeAtrribute"/></para>
    /// </typeparam>
    public class GenericModel<TIndex, TVertex> : ResourceHolder
        where TIndex : unmanaged
        where TVertex : unmanaged
    {
        internal readonly TVertex[] Vertices;
        internal readonly TIndex[]  Indices;

        public DeviceBuffer?  VertexBuffer { get; private set; }
        public DeviceBuffer? IndexBuffer { get; private set; }

        internal readonly IndexFormat IndexFormat;

        public GenericModel(TVertex[] vertices, TIndex[] indices)
        {
            Vertices = vertices;
            Indices = indices;

            Type vertexStructType = typeof(TVertex);

            var fieldInfos = vertexStructType.GetFields();

            var indexStructName = typeof(TIndex).Name;

            IndexFormat = indexStructName switch
            {
                nameof(UInt16) or nameof(Int16) => IndexFormat.UInt16,
                nameof(UInt32) or nameof(Int32) => IndexFormat.UInt32,
                _ => throw new ArgumentException($"The type {indexStructName} is not valid for {nameof(TIndex)}"),
            };
        }
        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            VertexBuffer = factory.CreateBuffer(new BufferDescription(VeldridUtils.GetSizeInBytes(Vertices), BufferUsage.VertexBuffer));
            VertexBuffer.Name = "VertexBuffer (generated)";
            graphicsDevice.UpdateBuffer(VertexBuffer, 0, Vertices);

            IndexBuffer = factory.CreateBuffer(new BufferDescription(VeldridUtils.GetSizeInBytes(Indices), BufferUsage.IndexBuffer));
            IndexBuffer.Name = "IndexBuffer (generated)";
            graphicsDevice.UpdateBuffer(IndexBuffer, 0, Indices);
        }
    }
}