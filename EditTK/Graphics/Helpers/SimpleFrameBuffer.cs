using EditTK.Graphics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Veldrid;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// Provides a framebuffer bundled with render textures, ready to use
    /// <para>see <see cref="Use(CommandList)"/></para>
    /// </summary>
    public class SimpleFrameBuffer : ResourceHolder
    {
        private readonly PixelFormat?   _depthFormat;
        private readonly PixelFormat[] _colorFormats;

        private          Texture?       _depthTexture;
        private readonly Texture?[]     _colorTextures;


        /// <summary>
        /// The output this 
        /// </summary>
        public OutputDescription OutputDescription { get; private set; }

        /// <summary>
        /// The render texture for the depth output or <see langword="null"/> if the framebuffer doesn't use depth
        /// </summary>
        public Texture? DepthTexture => _depthTexture;

        /// <summary>
        /// The render texture(s) for the color output(s)
        /// </summary>
        public Texture?[] ColorTextures => _colorTextures;

        public Framebuffer? Framebuffer => _framebuffer;

        private uint _width;
        private uint _height;
        private Framebuffer? _framebuffer;

        public SimpleFrameBuffer(PixelFormat? depthFormat, params PixelFormat[] colorFormats)
        {
            _depthFormat = depthFormat;
            _colorFormats = colorFormats;

            OutputDescription = new OutputDescription(
                _depthFormat.HasValue?new OutputAttachmentDescription(_depthFormat.Value) : null,
                _colorFormats.Select(x => new OutputAttachmentDescription(x)).ToArray()
                );
                

            _colorTextures = new Texture?[colorFormats.Length];
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            if (_width == 0 || _height == 0)
                return;

            #region local method
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateTexture(PixelFormat? format, ref Texture? texture, TextureUsage textureUsage)
            {
                texture?.Dispose();

                if (format == null)
                    return;

                texture = factory.CreateTexture(TextureDescription.Texture2D(
                    _width, _height, 1, 1, format.Value, textureUsage));
            }
            #endregion





            UpdateTexture(_depthFormat, ref _depthTexture, TextureUsage.DepthStencil | TextureUsage.Sampled);

            for (int i = 0; i < _colorFormats.Length; i++)
                UpdateTexture(_colorFormats[i], ref _colorTextures[i], TextureUsage.RenderTarget | TextureUsage.Sampled | TextureUsage.Storage);



            _framebuffer = factory.CreateFramebuffer(new FramebufferDescription(_depthTexture, _colorTextures));
        }

        /// <summary>
        /// Sets/Updates the dimensions of all RenderTextures, 
        /// needs to be called atleast once before using this <see cref="SimpleFrameBuffer"/>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <exception cref="ArgumentException"/>
        public void SetSize(uint width, uint height)
        {
            if (width == 0)  throw new ArgumentException($"{nameof(width)} can't be 0");
            if (height == 0) throw new ArgumentException($"{nameof(height)} can't be 0");

            _width = width;
            _height = height;

            UpdateResources();
        }

        /// <summary>
        /// Sets the current framebuffer to this 
        /// (makes all subsequent draw commands render to this framebuffer's render texture(s))
        /// <para>Note: All subsequent draw commands need to have a compatible <see cref="Veldrid.OutputDescription"/> 
        /// to the one of this <see cref="SimpleFrameBuffer"/></para>
        /// </summary>
        /// <param name="cl">The command list in which the framebuffer should be set</param>
        public void Use(CommandList cl)
        {
            cl.SetFramebuffer(_framebuffer);
        }
    }
}
