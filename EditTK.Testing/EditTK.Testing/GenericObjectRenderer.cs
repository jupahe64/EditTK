using EditTK.Graphics.Common;
using System;
using System.Numerics;
using Veldrid;

namespace EditTK.Testing
{
    public class GenericObjectRenderer<TIndex, TVertex>
        where TIndex : unmanaged
        where TVertex : unmanaged
    {
        public GenericModel<TIndex,TVertex> _model;
        public GenericModelRenderer<TIndex,TVertex> _modelRenderer;

        public GenericObjectRenderer(TVertex[] vertices, TIndex[] indices, byte[] vertexShaderBytes, byte[] fragmentShaderBytes,
            ShaderUniformLayout[] unformSetLayouts, OutputDescription outputDescription,
            BlendStateDescription? blendState = null, DepthStencilStateDescription? depthState = null,
            RasterizerStateDescription? rasterizerState = null)
        {
            _model = new GenericModel<TIndex, TVertex>(vertices, indices);
            _modelRenderer = new GenericModelRenderer<TIndex, TVertex>(
                vertexShaderBytes, fragmentShaderBytes, unformSetLayouts, outputDescription, blendState, depthState, rasterizerState);
        }

        public DeviceBuffer VertexBuffer => _model.VertexBuffer!;

        public void Draw(CommandList cl, params ResourceSet[] resourceSets)
        {
            _modelRenderer.Draw(cl, _model, resourceSets);
        }

        internal void CreateResources()
        {
            _modelRenderer.EnsureResourcesCreated();
        }

        internal ResourceSet CreateResourceSet(int setIndex, params (string, BindableResource)[] resources)
        {
            return _modelRenderer.UniformSetLayouts[setIndex].CreateResourceSet(resources);
        }

        internal ResourceSet CreateResourceSet(int setIndex, params (string, object)[] resources)
        {
            return _modelRenderer.UniformSetLayouts[setIndex].CreateResourceSet(resources);
        }

        internal ResourceSet CreateResourceSet(int setIndex, params BindableResource[] resources)
        {
            return _modelRenderer.UniformSetLayouts[setIndex].CreateResourceSet(resources);
        }
    }
}