using EditTK.Graphics;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Represents the source of a Texture, used in a Framebuffer
        /// </summary>
        public class RenderTexture : UpdateableResource<Texture?>
        {
            internal RenderTexture(PixelFormat? format, bool isDepth)
                : base(null)
            {
                Format = format;
                IsDepth = isDepth;
            }

            public Texture? Texture => Resource;

            public PixelFormat? Format { get; }
            public bool IsDepth { get; }

            public static implicit operator Texture?(RenderTexture rt) => rt.Texture;
        }


        private readonly PixelFormat?   _depthFormat;
        private readonly PixelFormat[] _colorFormats;

        

        private          RenderTexture       _depthTexture;
        private readonly RenderTexture[]     _colorTextures;

        private RenderTexture[] _myTextures;

        private Texture[] _colorTextureTempStorage;




        /// <summary>
        /// The <see cref="Veldrid.OutputDescription"/> of this <see cref="SimpleFrameBuffer"/>
        /// </summary>
        public OutputDescription OutputDescription { get; private set; }



        public RenderTexture DepthTexture  { get => _depthTexture;     init => UseExternalTexture(ref _depthTexture,     value); }
        public RenderTexture ColorTexture0 { get => _colorTextures[0]; init => UseExternalTexture(ref _colorTextures[0], value); }
        public RenderTexture ColorTexture1 { get => _colorTextures[1]; init => UseExternalTexture(ref _colorTextures[1], value); }
        public RenderTexture ColorTexture2 { get => _colorTextures[2]; init => UseExternalTexture(ref _colorTextures[2], value); }
        public RenderTexture ColorTexture3 { get => _colorTextures[3]; init => UseExternalTexture(ref _colorTextures[3], value); }
        public RenderTexture ColorTexture4 { get => _colorTextures[4]; init => UseExternalTexture(ref _colorTextures[4], value); }
        public RenderTexture ColorTexture5 { get => _colorTextures[5]; init => UseExternalTexture(ref _colorTextures[5], value); }
        public RenderTexture ColorTexture6 { get => _colorTextures[6]; init => UseExternalTexture(ref _colorTextures[6], value); }
        public RenderTexture ColorTexture7 { get => _colorTextures[7]; init => UseExternalTexture(ref _colorTextures[7], value); }
        
        public IReadOnlyList<RenderTexture> ColorTextures => _colorTextures;


        public Framebuffer? Framebuffer => _framebuffer;

        private uint _width;
        private uint _height;
        private Framebuffer? _framebuffer;
        private bool _isDirty;

        public SimpleFrameBuffer(PixelFormat? depthFormat, params PixelFormat[] colorFormats)
        {
            _depthFormat = depthFormat;
            _colorFormats = colorFormats;

            OutputDescription = new OutputDescription(
                _depthFormat.HasValue ? new OutputAttachmentDescription(_depthFormat.Value) : null,
                _colorFormats.Select(x => new OutputAttachmentDescription(x)).ToArray()
                );

            _colorTextures = _colorFormats.Select(x => new RenderTexture(x, isDepth: false)).ToArray();
            _depthTexture = new RenderTexture(depthFormat, isDepth: true);

            _myTextures = _colorTextures.Append(DepthTexture).ToArray();

            _colorTextureTempStorage = new Texture[colorFormats.Length];
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            if (_width == 0 || _height == 0)
                return;

            foreach (var item in _myTextures)
            {
                if (item.Format == null)
                    continue;

                TextureUsage textureUsage;

                if (item.IsDepth)
                    textureUsage = TextureUsage.DepthStencil | TextureUsage.Sampled;
                else
                    textureUsage = TextureUsage.RenderTarget | TextureUsage.Sampled | TextureUsage.Storage;

                item.Texture?.Dispose();
                item.Update(factory.CreateTexture(TextureDescription.Texture2D(
                    _width, _height, 1, 1, item.Format!.Value, textureUsage)));
            }

            MarkDirty();
        }


        private void UseExternalTexture(ref RenderTexture rt, RenderTexture newRt)
        {
            var formatInternal = rt.Format;
            var formatExternal = newRt.Format;

            if (formatInternal != formatExternal)
                throw new ArgumentException(
                    $"Format of renderTexture {formatExternal} does not match the format used in the framebuffer {formatInternal}");


            rt.Updated -= MarkDirty;

            rt = newRt;

            newRt.Updated += MarkDirty;
        }

        private void MarkDirty() => _isDirty = true;

        /// <summary>
        /// Sets/Updates the dimensions of all RenderTextures, 
        /// <para>needs to be called atleast once before using this <see cref="SimpleFrameBuffer"/> as it creates the textures in the first place</para>
        /// <para>if you want to use other render textures in this framebuffer see <see cref="Update(uint, uint, )"/></para>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <exception cref="ArgumentException"/>
        public void SetSize(uint width, uint height)
        {
            if (_width == width && _height == height) 
                return;

            if (width == 0) throw new ArgumentException($"{nameof(width)} can't be 0");
            if (height == 0) throw new ArgumentException($"{nameof(height)} can't be 0");

            _width = width;
            _height = height;

            UpdateResources();
        }


        private void CreateFramebuffer()
        {
            var factory = GraphicsAPI.ResourceFactory;

            System.Diagnostics.Debug.Assert(factory != null);

            void EnsureDimensionsAndFormatsMatch(RenderTexture rt)
            {
                var texture = rt.Texture;

                if (texture == null)
                    return;

                if (texture.Width != _width || texture.Height != _height)
                    throw new ArgumentException("Texture does not have the same Dimensions as the framebuffer");

                if (texture.Format != rt.Format)
                    throw new ArgumentException($"{nameof(RenderTexture)} has been updated with the wrong {nameof(PixelFormat)}, " +
                        $"expected: {rt.Format} got: {texture.Format}");
            }

            EnsureDimensionsAndFormatsMatch(DepthTexture);

            for (int i = 0; i < _colorTextures.Length; i++)
            {
                RenderTexture rt = _colorTextures[i];

                EnsureDimensionsAndFormatsMatch(rt);

                _colorTextureTempStorage[i] = rt.Texture!;
            }

            _framebuffer = factory.CreateFramebuffer(new FramebufferDescription(DepthTexture.Texture, _colorTextureTempStorage));

            _isDirty = false;
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
            if (_width == 0 || _height == 0)
                throw new InvalidOperationException($"Framebuffer has no specified Dimension, make sure to call {nameof(SetSize)}");

            if (_isDirty)
                CreateFramebuffer();

            cl.SetFramebuffer(_framebuffer);
        }
    }
}
