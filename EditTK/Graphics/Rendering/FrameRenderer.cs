using EditTK.Core.Common;
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


    public class TextureRef
    {
        internal string Name { get; init; } = string.Empty;
        internal PixelFormat Format { get; init; } = PixelFormat.R8_G8_B8_A8_UNorm;
    }

    public class PixelBufferRef
    {
        internal string Name { get; init; } = string.Empty;
        internal PixelFormat Format { get; init; } = PixelFormat.R8_G8_B8_A8_UNorm;
    }

    public class FrameRenderer
    {
        public IObjectHolder? ObjectHolder { get; set; }
        public ResourceSet CommonResourceSet { get; private set; }

        private readonly Framebuffer[] _framebuffers;
        private readonly Dictionary<TextureRef, Texture?> _textures = new();

        private Framebuffer? _swapchainFramebuffer;
        private Framebuffer? _depthCopyFramebuffer;
        private Texture? _depthCopyTexture;
        private GenericModelRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _fullscreenQuadRenderer;
        private GenericModelRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _depthCopyQuadRenderer;
        private ResourceSet[]? _fullscreenQuadParamsSets;
        private ResourceSet[]? _depthCopyQuadParamsSets;

        private CommandList? _cl;

        private List<(FrameRenderBuilder.DeviceTextureInfo textureInfo, TextureRef[] slots)> _textureSlotGroups;
        private List<(FrameRenderBuilder.FrameBufferInfo info, int[] framebufferSlots)> _framebufferSlotGroups;



        public IReadOnlyList<Framebuffer> Framebuffers => _framebuffers;
        public IReadOnlyDictionary<TextureRef, Texture?> Textures => _textures;


        public FrameRenderer(FrameRenderBuilder builder, Vector2 viewportSize, IObjectHolder? objectHolder = null)
        {
            var textureSlotGroups = builder.GetRenderTextureSlotGroups();

            var slotRemapping = new Dictionary<int, int>(textureSlotGroups.SelectMany(
                x => x.slots.Select(y => new KeyValuePair<int,int>(y, x.slots[0]))
                ));

            _framebufferSlotGroups = builder.GetFramebufferSlotGroups(slotRemapping);

            _textureSlotGroups = textureSlotGroups.Select(x => (
                x.textureInfo,
                x.slots.Select(y => builder.RenderTextureSlots[y]).ToArray()
                )).ToList();

            _framebuffers = new Framebuffer[builder.FramebufferSlotCount];

            ObjectHolder = objectHolder;
        }

        public void Resize(Vector2 newSize)
        {

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

            ObjectHolder?.ForEachObject(obj => (obj as IDrawable)?.CreateGraphicsResources(factory));

            _cl = factory.CreateCommandList();


        }
    }
}
