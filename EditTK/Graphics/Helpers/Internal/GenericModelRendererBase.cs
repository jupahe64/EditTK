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
        private readonly ShaderSource _vertexShaderSource;
        private readonly ShaderSource _fragmentShaderSource;
        private readonly ShaderUniformLayout[] _uniformLayouts;
        private BlendStateDescription? _blendState;
        private DepthStencilStateDescription? _depthState;
        private RasterizerStateDescription? _rasterizerState;

        protected Pipeline? _pipeline;
        private OutputDescription _outputDescription;
        private ShaderSetDescription _shaderSet;
        private ResourceLayout[]? _resourceLayouts;

        public IReadOnlyList<ShaderUniformLayout> UniformSetLayouts => _uniformLayouts;

        
        public GenericModelRendererBase(ShaderSource vertexShaderSource, ShaderSource fragmentShaderSource,
            ShaderUniformLayout[] uniformLayouts, OutputDescription outputDescription,
            BlendStateDescription? blendState = null, DepthStencilStateDescription? depthState = null,
            RasterizerStateDescription? rasterizerState = null)
        {
            vertexShaderSource.Updated += CreatePipeline;
            fragmentShaderSource.Updated += CreatePipeline;

            _vertexShaderSource = vertexShaderSource;
            _fragmentShaderSource = fragmentShaderSource;
            _uniformLayouts = uniformLayouts;
            _outputDescription = outputDescription;
            _blendState = blendState;
            _depthState = depthState;
            _rasterizerState = rasterizerState;
        }

        protected abstract VertexLayoutDescription[] GetVertexLayouts();

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            _resourceLayouts = new ResourceLayout[_uniformLayouts.Length];

            for (int i = 0; i < _uniformLayouts.Length; i++)
            {
                _uniformLayouts[i].EnsureResourcesCreated();

                ResourceLayout? layout = _uniformLayouts[i].ResourceLayout;

                Debug.Assert(layout != null);
                _resourceLayouts[i] = layout;
            }

            CreatePipeline();
        }

        
        private void CreatePipeline()
        {
            var factory = GraphicsAPI.ResourceFactory;

            Debug.Assert(factory != null);

            if (_shaderSet.Shaders != null)
            {
                for (int i = 0; i < _shaderSet.Shaders.Length; i++)
                {
                    _shaderSet.Shaders[i]?.Dispose();
                }
            }

            _shaderSet = new ShaderSetDescription(
                GetVertexLayouts(),
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, _vertexShaderSource.ShaderBytes, "main", true),
                    new ShaderDescription(ShaderStages.Fragment, _fragmentShaderSource.ShaderBytes, "main", true)));

            _pipeline?.Dispose();

            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                _blendState ?? BlendStateDescription.SingleOverrideBlend,
                _depthState ?? DepthStencilStateDescription.DepthOnlyLessEqual,
                _rasterizerState ?? RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                _shaderSet,
                _resourceLayouts,
                _outputDescription));
        }
    }
}