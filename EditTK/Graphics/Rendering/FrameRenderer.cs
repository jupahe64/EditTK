using EditTK.Core.Common;
using EditTK.UI;
using EditTK.Graphics.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Graphics.Rendering
{

    //TODO


    public class RenderTextureRef
    {
        internal string Name { get; init; } = string.Empty;
        internal PixelFormat Format { get; init; } = PixelFormat.R8_G8_B8_A8_UNorm;
    }

    public delegate void FrameRenderInstruction(FrameRenderer frameRenderer, CommandList cl);

    public delegate ResourceSet CompositionResourceSetup(Texture outputTexture, FrameRenderer frameRenderer);

    public class FrameRenderer
    {
        public IObjectHolder? ObjectHolder { get; set; }
        public ResourceSet CommonResourceSet { get; private set; }

        private readonly Framebuffer[] _framebuffers;
        private readonly Dictionary<RenderTextureRef, Texture?> _textures = new();
        private readonly ResourceSet[] _compResources;
        private readonly RenderTextureRef _outputTextureRef;
        private Framebuffer? _swapchainFramebuffer;
        private Framebuffer? _depthCopyFramebuffer;
        private Texture? _depthCopyTexture;
        private GenericModelRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _fullscreenQuadRenderer;
        private GenericModelRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _depthCopyQuadRenderer;
        private ResourceSet[]? _fullscreenQuadParamsSets;
        private ResourceSet[]? _depthCopyQuadParamsSets;

        private CommandList? _cl;

        private (FrameRenderBuilder.DeviceTextureInfo textureInfo, RenderTextureRef[] slots)[] _textureSlotGroups;
        private (FrameRenderBuilder.FrameBufferInfo info, int[] framebufferSlots)[] _framebufferSlotGroups;

        private CompositionResourceSetup[] _compResourceSetups;



        public IReadOnlyList<Framebuffer> Framebuffers => _framebuffers;
        public IReadOnlyDictionary<RenderTextureRef, Texture?> Textures => _textures;
        public IReadOnlyList<ResourceSet> CompResources => _compResources;


        public FrameRenderer(FrameRenderBuilder builder, RenderTextureRef outputTexture)
        {
            var textureSlotGroups = builder.GetRenderTextureSlotGroups();

            var slotRemapping = new Dictionary<RenderTextureRef, RenderTextureRef>(textureSlotGroups.SelectMany(
                x => x.slots.Select(y => new KeyValuePair<RenderTextureRef, RenderTextureRef>(
                    builder.RenderTextureSlots[y], builder.RenderTextureSlots[x.slots[0]]))
                ));

            _framebufferSlotGroups = builder.GetFramebufferSlotGroups(slotRemapping);

            _textureSlotGroups = textureSlotGroups.Select(x => (
                x.textureInfo,
                x.slots.Select(y => builder.RenderTextureSlots[y]).ToArray()
                )).ToArray();

            _framebuffers = new Framebuffer[builder.FramebufferSlotCount];

            _compResourceSetups = builder.CompResourceSetups.ToArray();

            _outputTextureRef = outputTexture;
        }

        public void SetSize(Vector2 size)
        {
            uint width = (uint)size.X;
            uint height = (uint)size.Y;

            foreach (var (info, slots) in _textureSlotGroups)
            {
                TextureUsage renderUsage = (info.Format is PixelFormat.D24_UNorm_S8_UInt or PixelFormat.D32_Float_S8_UInt)
                    ? TextureUsage.DepthStencil : TextureUsage.RenderTarget;

                var texture = GraphicsAPI.ResourceFactory!.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, info.Format,
                    TextureUsage.Sampled | renderUsage));

                texture.Name = info.Name;

                foreach (var textureRef in slots)
                {
                    _textures[textureRef] = texture;
                }
            }

            foreach (var (info, slots) in _framebufferSlotGroups)
            {
                var framebuffer = GraphicsAPI.ResourceFactory!.CreateFramebuffer(new FramebufferDescription(
                    info.requestedDepthTex == null ? null : _textures[info.requestedDepthTex],
                    info.requestedColorTexs.Select(x => _textures[x]).ToArray())
                    );

                foreach (var slot in slots)
                {
                    _framebuffers[slot] = framebuffer;
                }
            }

            for (int i = 0; i < _compResourceSetups.Length; i++)
            {
                _compResources[i] = _compResourceSetups[i](_textures[_outputTextureRef], this);
            }
        }


        protected void CreateResources(ResourceFactory factory)
        {
            Debug.Assert(GraphicsAPI.GD != null);

            VeldridUtils.VertexFullscreenQuad[] verts = VeldridUtils.GetFullScreenQuadVerts(GraphicsAPI.GD);

            //_fullscreenQuadRenderer = new GenericModelRenderer<ushort, Util.VertexFullscreenQuad>(verts, Util.QuadIndices, Encoding.UTF8.GetBytes(VertexCodeFullscreenQuad), Encoding.UTF8.GetBytes(FragmentCodeFullscreenQuad),
            //    depthState: DepthStencilStateDescription.Disabled, shaderResourceLayouts: new ResourceLayoutDescription[]{
            //            new ResourceLayoutDescription(
            //                    new ResourceLayoutElementDescription("Color0Texture",    ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("Color1Texture",    ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("Depth0Texture",    ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("Depth1Texture",    ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("TextureSampler",   ResourceKind.Sampler,         ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("Picking1Texture",  ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("PickingSampler",   ResourceKind.Sampler,         ShaderStages.Fragment)
            //                )
            //        });

            //_fullscreenQuadRenderer.CreateResources(factory, graphicsDevice, graphicsDevice.SwapchainFramebuffer.OutputDescription);




            _depthCopyTexture = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
            //_sceneDepthStagingTexture = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_Float, TextureUsage.Staging));

            //_depthCopyFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(null, _depthCopyTexture));


            //_depthCopyQuadRenderer = new GenericModelRenderer<ushort, Util.VertexFullscreenQuad>(verts, Util.QuadIndices, Encoding.UTF8.GetBytes(VertexCodeDepthCopyQuad), Encoding.UTF8.GetBytes(FragmentCodeDepthCopyQuad),
            //    depthState: DepthStencilStateDescription.Disabled, shaderResourceLayouts: new ResourceLayoutDescription[]{
            //            new ResourceLayoutDescription(
            //                    new ResourceLayoutElementDescription("DepthTexture",   ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //                    new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler,         ShaderStages.Fragment)
            //                )
            //        });

            //_depthCopyQuadRenderer.CreateResources(factory, graphicsDevice, _depthCopyFramebuffer.OutputDescription);

            //CreateWindowSizeBoundResources(factory);


            //_ubView = factory.CreateBuffer(new BufferDescription(Util.SIZE_OF_MAT4, BufferUsage.UniformBuffer));
            //_ubCameraPlane = factory.CreateBuffer(new BufferDescription(Util.SIZE_OF_VEC4, BufferUsage.UniformBuffer));
            //ubViewportSize_ForceSolidHighlight = factory.CreateBuffer(new BufferDescription(Util.MinimumBufferSize(Util.SIZE_OF_VEC2), BufferUsage.UniformBuffer));

            //_sceneParamsSet = factory.CreateResourceSet(new ResourceSetDescription(
            //    SharedResources.SceneParamsLayout,
            //    _ubView,
            //    _ubCameraPlane,
            //    ubViewportSize_ForceSolidHighlight));

            //Debug.Assert(_sceneFramebuffer != null);

            //ObjectHolder?.ForEachObject(obj => (obj as IDrawable)?.CreateGraphicsResources(factory));

            //_cl = factory.CreateCommandList();


        }
    }
}
