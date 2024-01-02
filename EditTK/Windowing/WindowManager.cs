using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.ImGui;
using Silk.NET.WebGPU.Safe;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace EditTK.Windowing
{
    public static class WindowManager
    {
        private record struct WindowResources(ImGuiController ImguiController, SurfacePtr Surface);

        private static bool s_isRunning = false;

        private static readonly List<IWindow> s_pendingInits = new();
        private static readonly List<(IWindow window, WindowResources res)> s_windows = new();

        public static void CreateWindow(Vector2D<int> size, InstancePtr instance, AdapterPtr adapter, DevicePtr device,
            Action<double, TextureViewPtr, ImGuiController> renderWgpuDelegate, out IWindow window, PresentMode presentMode = PresentMode.Fifo)
        {
            var options = WindowOptions.Default;
            options.API = GraphicsAPI.None;
            options.Size = size;
            options.IsVisible = false;

            window = Window.Create(options);

            var _window = window;

            window.Load += () =>
            {
                //initialization
                if(_window.Native!.Win32.HasValue)
                    WindowsDarkmodeUtil.SetDarkmodeAware(_window.Native.Win32.Value.Hwnd);

                var surface = _window.CreateWebGPUSurface(instance.GetAPI(), instance);

                TextureFormat swapChainFormat = surface.GetPreferredFormat(adapter);

                var input = _window.CreateInput();
                var imguiController = new ImGuiController(device, swapChainFormat, _window, input, () => { });

                var _last_framebufferSize = new Vector2D<int>();
                TextureViewPtr? _swapChainView = null;


                //rendering/update
                _window.Update += ds => imguiController.Update((float)ds);

                var isRequestShow = true;
                _window.Render += (deltaSeconds) =>
                {
                    var framebufferSize = _window.FramebufferSize;

                    if (framebufferSize.X * framebufferSize.Y == 0)
                        return;

                    if (_last_framebufferSize != framebufferSize)
                    {
                        _last_framebufferSize = framebufferSize;

                        surface.Configure(new Silk.NET.WebGPU.Safe.SurfaceConfiguration
                        {
                            AlphaMode = CompositeAlphaMode.Opaque,
                            Device = device,
                            Usage = TextureUsage.RenderAttachment,
                            Format = swapChainFormat,
                            ViewFormats = new TextureFormat[] { swapChainFormat },
                            Width = (uint)framebufferSize.X,
                            Height = (uint)framebufferSize.Y,
                            PresentMode = PresentMode.Immediate
                        });
                    }

                    _swapChainView?.Release();

                    var (swapChainTexture, _, status) = surface.GetCurrentTexture();

                    Debug.Assert(status == SurfaceGetCurrentTextureStatus.Success);

                    _swapChainView = swapChainTexture.CreateView(
                        swapChainFormat,
                        TextureViewDimension.Dimension2D, TextureAspect.All,
                        baseMipLevel: 0, mipLevelCount: 1,
                        baseArrayLayer: 0, arrayLayerCount: 1, label: "SwapchainView");

                    imguiController.MakeCurrent();

                    renderWgpuDelegate.Invoke(deltaSeconds, _swapChainView.Value, imguiController);

                    surface.Present();

                    if (isRequestShow)
                    {
                        _window.IsVisible = true;
                        isRequestShow = false;
                    }
                };

                s_windows.Add((_window, new WindowResources(imguiController, surface)));
            };

            s_pendingInits.Add(window);
        }

        public static void Run()
        {
            if (s_isRunning)
                return;

            s_isRunning = true;

            while (s_windows.Count > 0 || s_pendingInits.Count > 0)
            {
                if (s_pendingInits.Count > 0)
                {
                    foreach (var window in s_pendingInits)
                        window.Initialize();

                    s_pendingInits.Clear();
                }


                for (int i = 0; i < s_windows.Count; i++)
                {
                    var (window, res) = s_windows[i];

                    window.DoEvents();
                    if (!window.IsClosing)
                    {
                        window.DoUpdate();
                    }

                    if (!window.IsClosing && 
                        (window.FramebufferSize.X * window.FramebufferSize.Y) > 0)
                    {
                        window.DoRender();
                    }

                    if (window.IsClosing)
                    {
                        s_windows.RemoveAt(i);

                        window.DoEvents();
                        window.Reset();

                        res.Surface.Unconfigure();
                        res.Surface.Release();
                        res.ImguiController.Dispose();

                        i--;
                    }
                }
            }
        }
    }
}