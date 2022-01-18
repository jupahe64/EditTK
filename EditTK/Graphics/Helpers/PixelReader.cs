using EditTK.Graphics;
using EditTK.Util;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using Veldrid;

using static EditTK.Graphics.GraphicsAPI;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// Hides the type parameter of <see cref="PixelReader{TPixel}"/> to be used in Collections or other Contexts where the type isn't known ahead of time
    /// </summary>
    public interface IPixelReader
    {
        public Type GenericType { get; }

        public void ReadPixel<TPixel>(CommandList cl, Texture texture, uint x, uint y, Fence commandListFence, PixelReader<TPixel>.PixelCallBack onPixelRead)
            where TPixel : unmanaged
        {
            if(this is PixelReader<TPixel> reader)
            {
                reader.ReadPixel(cl, texture, x, y, commandListFence, onPixelRead);
            }
            else
            {
                throw new InvalidOperationException($"The generic type of {nameof(TPixel)} {typeof(TPixel)} " +
                    $"does not match the generic type of the {GetType()}");
            }
        }
    }

    /// <summary>
    /// Can read pixels from a given texture of the matching format
    /// <para>see <see cref="ReadPixel(CommandList, Texture, uint, uint, Fence, PixelReader{TPixel}.PixelCallBack)"/></para>
    /// </summary>
    /// <typeparam name="TPixel"></typeparam>
    public class PixelReader<TPixel> : ResourceHolder, IPixelReader
        where TPixel : unmanaged
    {
        public Type GenericType => typeof(TPixel);

        public delegate void PixelCallBack(TPixel pixel);

        #region threading
        private AutoResetEvent _newPixelToRead = new AutoResetEvent(false);
        private Fence? _commandListFence;
        private PixelCallBack? _onPixelRead;


        private void ReaderThread()
        {
            while (true)
            {
                _newPixelToRead.WaitOne();

                using(GraphicsAPI.ProtectGraphicsDevice())
                {
                    var before = GD;

                    if (!GD!.WaitForFence(_commandListFence, new TimeSpan(0, 0, 0, 
                        seconds: 0, milliseconds:100)))
                        continue;

                    if (GD != before)
                        continue;

                    var view = GD.Map<TPixel>(_stagingTexture, MapMode.Read);
                    TPixel pixel = view[0];
                    GD.Unmap(_stagingTexture);
                    _onPixelRead!(pixel);
                }
            }
        }
        #endregion


        #region depth copy resources
        private readonly Dictionary<Texture, ResourceSet> _depthCopySets = new Dictionary<Texture, ResourceSet>();

        private readonly static ComputeShader? s_depthCopyCompute = new ComputeShader(
                    computeShaderSource: Encoding.UTF8.GetBytes(@"
                    #version 450

                    layout(set = 0, binding = 0) uniform ub_Coords
                    {
                        vec2 Coords;
                    };

                    layout(set = 0, binding = 1, r32f) uniform image2D Out;

                    layout(set = 0, binding = 2) uniform texture2D  In;
                    layout(set = 0, binding = 3) uniform sampler    LS;

                    layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

                    void main()
                    {
                        float depth = texelFetch(sampler2D(In, LS), ivec2(Coords), 0).r;
                        imageStore(Out,ivec2(gl_GlobalInvocationID.xy),vec4(depth,0,0,0));
                    }
                    "),
                    new ShaderUniformLayout[]
                    {
                    ShaderUniformLayoutBuilder.Get()
                    .BeginUniformBuffer("ub_Coords", ShaderStages.Compute)
                        .AddUniform<Vector2>("Coords")
                    .EndUniformBuffer()
                    .AddImage("Out", ShaderStages.Compute)
                    .AddTexture("In", ShaderStages.Compute)
                    .AddSampler("LS", ShaderStages.Compute)
                    .GetLayout()
                    }, 1, 1, 1);

        private Texture? _depthCopyTexture;
        private DeviceBuffer? _depthCopyCoordBuffer;
        #endregion

        private bool _isDepthFormat = false;
        private readonly PixelFormat _readFormat;
        private Texture? _stagingTexture;

        public PixelReader(PixelFormat format)
        {
            _isDepthFormat = (format == PixelFormat.D24_UNorm_S8_UInt || format == PixelFormat.D32_Float_S8_UInt);

            _readFormat = _isDepthFormat ? PixelFormat.R32_Float : format;

            new Thread(ReaderThread).Start();
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            if(_isDepthFormat && (_depthCopyTexture==null || _depthCopyTexture.IsDisposed))
            {
                _depthCopyTexture = factory.CreateTexture(TextureDescription.Texture2D(
                    1, 1, 1, 1, _readFormat, TextureUsage.Storage | TextureUsage.Sampled));

                _depthCopySets.Clear(); //clean up, these resources (if any) are gone for good

                _depthCopyCoordBuffer = VeldridUtils.CreateUniformBuffer(Vector2.Zero);
            }

            _stagingTexture = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, _readFormat, TextureUsage.Staging));

            _stagingTexture.Name = "Staging Texture";
        }

        /// <summary>
        /// Reads a pixel from <paramref name="texture"/> 
        /// and calls <paramref name="onPixelRead"/> with the read pixel when done
        /// </summary>
        /// <param name="cl">The <see cref="CommandList"/> to use for all involved gpu commands</param>
        /// <param name="texture"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="commandListFence">The <see cref="Fence"/> asociated with the commandList used</param>
        /// <param name="onPixelRead">Gets called from a seperate thread when the pixel is read</param>
        public void ReadPixel(CommandList cl, Texture texture, uint x, uint y, Fence commandListFence, PixelCallBack onPixelRead)
        {
            EnsureResourcesCreated();

            if(!GD!.IsUvOriginTopLeft)
                y = texture.Height-1-y;

            if (_isDepthFormat)
            {
                Vector2 coords = new Vector2(x, y);

                cl.UpdateBuffer(_depthCopyCoordBuffer, 0, coords);


                var set = _depthCopySets.GetOrCreate(texture, () => s_depthCopyCompute?.UniformSetLayouts[0].CreateResourceSet(
                    ("ub_Coords", _depthCopyCoordBuffer!),
                    ("Out", _depthCopyTexture!),
                    ("In", texture),
                    ("LS", GD.LinearSampler)
                ));

                s_depthCopyCompute!.Dispatch(cl, 1, 1, 1, set);

                cl.CopyTexture(_depthCopyTexture, 0, 0, 0, 0, 0,
                   _stagingTexture, 0, 0, 0, 0, 0, 1, 1, 1, 1);
            }

            else
            {
                cl.CopyTexture(texture, x, y, 0, 0, 0,
                   _stagingTexture, 0, 0, 0, 0, 0, 1, 1, 1, 1);
            }

            _commandListFence = commandListFence;
            _onPixelRead = onPixelRead;

            _newPixelToRead.Set();
        }
    }
}
