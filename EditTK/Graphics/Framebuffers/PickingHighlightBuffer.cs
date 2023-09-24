using Silk.NET.WebGPU;

using Silk.NET.WebGPU.Safe;
using static EditTK.Graphics.Framebuffer;

namespace EditTK.Graphics.Framebuffers
{
    public class PickingHighlightBuffer
    {
        Framebuffer _framebuffer;

        public uint Width => _framebuffer.Width;
        public uint Height => _framebuffer.Height;

        public TextureFormat ObjIdChannelFormat => _framebuffer.GetFormat(0);
        public TextureFormat HighlightChannelFormat => _framebuffer.GetFormat(1);
        public TextureFormat OutlineChannelFormat => _framebuffer.GetFormat(2);
        public TextureFormat DepthStencilFormat => _framebuffer.DepthStencilFormat!.Value;

        private PickingHighlightBuffer(Framebuffer framebuffer)
        {
            _framebuffer = framebuffer;
        }

        public static PickingHighlightBuffer Create(DevicePtr device,
            TextureFormat objIdFormat, TextureFormat highlightFormat, TextureFormat outlineFormat,
            TextureFormat depthStencilFormat, uint width = 0, uint height = 0, string? label = null)
        {
            var framebuffer = new Framebuffer(device, new (string name, TextureFormat format)[]
            {
                ("ObjID", objIdFormat),
                ("Highlight", highlightFormat),
                ("Outline", outlineFormat),
            }, depthStencilFormat, label);

            var gbuffer = new PickingHighlightBuffer(framebuffer);

            gbuffer.EnsureSize(width, height);

            return gbuffer;
        }

        public bool TryGetTargets(out ColorTargetView objID, out ColorTargetView highlightColor, out ColorTargetView outline,
            out DepthStencilTargetView depthStencil)
        {
            var result = true;

            result &= _framebuffer.TryGetColorTarget(0, out objID);
            result &= _framebuffer.TryGetColorTarget(1, out highlightColor);
            result &= _framebuffer.TryGetColorTarget(2, out outline);
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