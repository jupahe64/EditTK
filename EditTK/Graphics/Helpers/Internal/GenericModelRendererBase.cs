using System.Collections.Generic;
using System.Diagnostics;
using Veldrid;
using Veldrid.SPIRV;

namespace EditTK.Graphics.Helpers.Internal
{
    /// <summary>
    /// Contains the base functionality of <see cref="GenericModelRenderer{,}"/> and <see cref="GenericInstanceRenderer{,,}"/>
    /// </summary>
    /// <typeparam name="TIndex"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public abstract class GenericModelRendererBase<TIndex, TVertex> : ResourceHolder
        where TIndex : unmanaged
        where TVertex : unmanaged
    {
        private readonly byte[] _vertexShaderBytes;
        private readonly byte[] _fragmentShaderBytes;
        private readonly ShaderUniformLayout[] _uniformLayouts;
        private BlendStateDescription? _blendState;
        private DepthStencilStateDescription? _depthState;
        private RasterizerStateDescription? _rasterizerState;

        protected Pipeline? _pipeline;
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
        public GenericModelRendererBase(byte[] vertexShaderBytes, byte[] fragmentShaderBytes,
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

        protected abstract VertexLayoutDescription[] GetVertexLayouts();

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            _shaderSet = new ShaderSetDescription(
                GetVertexLayouts(),
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, _vertexShaderBytes, "main", true),
                    new ShaderDescription(ShaderStages.Fragment, _fragmentShaderBytes, "main", true)));


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
    }
}