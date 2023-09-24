using Silk.NET.WebGPU;

using Silk.NET.WebGPU.Safe;
using static EditTK.Graphics.Framebuffer;

namespace EditTK.Graphics.Framebuffers
{
    public class GBuffer
    {
        Framebuffer _framebuffer;

        public uint Width => _framebuffer.Width;
        public uint Height => _framebuffer.Height;

        public TextureFormat AlbedoChannelFormat => _framebuffer.GetFormat(0);
        public TextureFormat NormalChannelFormat => _framebuffer.GetFormat(1);
        public TextureFormat LightChannelFormat => _framebuffer.GetFormat(2);
        public TextureFormat DepthStencilFormat => _framebuffer.DepthStencilFormat!.Value;

        private GBuffer(Framebuffer framebuffer)
        {
            _framebuffer = framebuffer;
        }

        public static GBuffer Create(DevicePtr device,
            TextureFormat albedoFormat, TextureFormat normalFormat, TextureFormat lightFormat,
            TextureFormat depthStencilFormat, uint width = 0, uint height = 0, string? label = null)
        {
            var framebuffer = new Framebuffer(device, new (string name, TextureFormat format)[]
            {
                ("Albedo", albedoFormat),
                ("Normal", normalFormat),
                ("Light", lightFormat),
            }, depthStencilFormat, label);

            var gbuffer = new GBuffer(framebuffer);

            gbuffer.EnsureSize(width, height);

            return gbuffer;
        }

        public bool TryGetTargets(out ColorTargetView albedo, out ColorTargetView normal, out ColorTargetView light,
            out DepthStencilTargetView depthStencil)
        {
            var result = true;

            result &= _framebuffer.TryGetColorTarget(0, out albedo);
            result &= _framebuffer.TryGetColorTarget(1, out normal);
            result &= _framebuffer.TryGetColorTarget(2, out light);
            result &= _framebuffer.TryGetDepthTarget(out var depthStencilTarget);
            depthStencil = depthStencilTarget.GetValueOrDefault();

            return result;
        }

        public void EnsureSize(uint width, uint height)
        {
            _framebuffer.EnsureSize(width, height);
        }
    }
}