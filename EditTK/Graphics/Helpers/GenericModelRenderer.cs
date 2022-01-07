using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace EditTK.Graphics.Helpers
{
    internal static class VertexFormatCache
    {
        private static readonly Dictionary<Type, VertexLayoutDescription[]> _vertexLayoutsPerType = new();

        public static VertexLayoutDescription[] GetVertexLayoutAsArray<T>()
        {
            Type type = typeof(T);

            if (!_vertexLayoutsPerType.TryGetValue(type, out VertexLayoutDescription[]? layout))
            {
                var fieldInfos = type.GetFields();

                var elementDescriptions = new VertexElementDescription[fieldInfos.Length];

                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    var attr = fieldInfos[i].GetCustomAttribute<VertexAttributeAtrribute>();

                    if (attr == null)
                        throw new ArgumentException($"Field {fieldInfos[i].Name} of the struct {type.Name} has no {nameof(VertexAttributeAtrribute)}");

                    elementDescriptions[i] = new VertexElementDescription(attr.AttributeName, VertexElementSemantic.TextureCoordinate, attr.AttributeFormat);
                }

                layout = new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(elementDescriptions)
                };


                _vertexLayoutsPerType[type] = layout;
            }

            return layout;
        }

        public static VertexLayoutDescription GetVertexLayout<T>() => GetVertexLayoutAsArray<T>()[0];
    }

    /// <summary>
    /// Provides all information to draw/render a specific type of model
    /// <para>see <see cref="Draw(CommandList, GenericModel{TIndex, TVertex}, ResourceSet[])"/></para>
    /// </summary>
    /// <typeparam name="TIndex">The index type used in the IndexBuffer</typeparam>
    /// <typeparam name="TVertex">The vertex type used in the VertexBuffer
    ///                     <para>Note: All fields need to have a <see cref="VertexAttributeAtrribute"/></para>
    /// </typeparam>
    public class GenericModelRenderer<TIndex, TVertex> : ResourceHolder
        where TIndex : unmanaged
        where TVertex : unmanaged
    {
        private readonly byte[] _vertexShaderBytes;
        private readonly byte[] _fragmentShaderBytes;
        private readonly ShaderUniformLayout[] _uniformLayouts;
        private BlendStateDescription? _blendState;
        private DepthStencilStateDescription? _depthState;
        private RasterizerStateDescription? _rasterizerState;

        private Pipeline? _pipeline;
        private OutputDescription _outputDescription; 
        private ShaderSetDescription _shaderSet;

        public IReadOnlyList<ShaderUniformLayout> UniformSetLayouts => _uniformLayouts;

        /// <summary>
        /// Creates a new <see cref="GenericModelRenderer{TIndex, TVertex}"/>
        /// </summary>
        /// <param name="vertexShaderBytes">The vertex shader code in bytes</param>
        /// <param name="fragmentShaderBytes">The fragment shader code in bytes</param>
        /// <param name="uniformLayouts">The layouts of all uniform sets used in the shader(s), 
        /// the order should match the set slot in the shader!</param>
        /// <param name="outputDescription">Describes the color/depth outputs of the fragment shader</param>
        /// <param name="blendState">Describes the blending options when drawing</param>
        /// <param name="depthState">Describes the depth compare/write behaivior when drawing</param>
        /// <param name="rasterizerState">Describes misc. render options</param>
        public GenericModelRenderer(byte[] vertexShaderBytes, byte[] fragmentShaderBytes,
            ShaderUniformLayout[] uniformLayouts, OutputDescription outputDescription,
            BlendStateDescription? blendState = null, DepthStencilStateDescription? depthState = null,
            RasterizerStateDescription? rasterizerState = null)
        {
            _vertexShaderBytes = vertexShaderBytes;
            _fragmentShaderBytes = fragmentShaderBytes;
            _uniformLayouts = uniformLayouts;
            _outputDescription = outputDescription;
            _blendState = blendState;
            _depthState = depthState;
            _rasterizerState = rasterizerState;
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            _shaderSet = new ShaderSetDescription(
                VertexFormatCache.GetVertexLayoutAsArray<TVertex>(),
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, _vertexShaderBytes, "main", true),
                    new ShaderDescription(ShaderStages.Fragment, _fragmentShaderBytes, "main", true)));


            //var res = SpirvCompilation.CompileVertexFragment(
            //    _vertexShaderBytes,
            //    _fragmentShaderBytes,
            //    CrossCompileTarget.HLSL
            //    );

            var resourceLayouts = new ResourceLayout[_uniformLayouts.Length];

            for (int i = 0; i < _uniformLayouts.Length; i++)
            {
                _uniformLayouts[i].EnsureResourcesCreated();

                ResourceLayout? layout = _uniformLayouts[i].ResourceLayout;

                Debug.Assert(layout != null);
                resourceLayouts[i] = layout;
            }

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                _blendState ?? BlendStateDescription.SingleOverrideBlend,
                _depthState ?? DepthStencilStateDescription.DepthOnlyLessEqual,
                _rasterizerState ?? RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                _shaderSet,
                resourceLayouts,
                _outputDescription));
        }


        /// <summary>
        /// Draws a <see cref="GenericModel{TIndex, TVertex}"/> 
        /// with the shaders and renderstate of this <see cref="GenericModelRenderer{TIndex, TVertex}"/>
        /// and the given resourceSets
        /// </summary>
        /// <param name="cl">The <see cref="CommandList"/> to use for all involved gpu commands</param>
        /// <param name="model">The model to draw</param>
        /// <param name="resourceSets">The resource sets to submit to the shader(s), 
        /// the order should match the set slot in the shader!</param>
        public void Draw(CommandList cl, GenericModel<TIndex,TVertex> model, params ResourceSet[] resourceSets)
        {
            EnsureResourcesCreated();

            model.EnsureResourcesCreated();

            cl.SetPipeline(_pipeline);


            for (int i = 0; i < resourceSets.Length; i++)
            {
                cl.SetGraphicsResourceSet((uint)i, resourceSets[i]);
            }

            cl.SetVertexBuffer(0, model.VertexBuffer);
            cl.SetIndexBuffer(model.IndexBuffer, model.IndexFormat);

            cl.DrawIndexed((uint)model.Indices.Length);
        }
    }


    

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