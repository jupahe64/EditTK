using EditTK.Graphics;
using EditTK.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace EditTK
{
    class DeviceChangedException : Exception { }

    /// <summary>
    /// Manages all <see cref="VeldridSDLWindow"/>s and provides functions for setting the global state
    /// </summary>
    public static class WindowManager
    {
        private static List<VeldridSDLWindow> _windows = new();

        private static bool _running = false;
        private static string? _defaultFont_filePath;
        private static float _defaultFont_size;
        private static float _globalFontScale = 1f;
        private static bool _defaultFont_isOversample;
        private static GraphicsBackend? _requestedBackend;


        public static bool MoreThanOneWindow() => _windows.Count > 1;

        public static void RequestBackend(GraphicsBackend graphicsBackend)
        {
            if (GraphicsAPI.GD?.BackendType == graphicsBackend)
                return;

            bool update = _requestedBackend != null;

            _requestedBackend = graphicsBackend;

            if (update)
            {
                Input.InputTracker.Reset();

                GraphicsAPI.SetGraphicsDevice(null);

                foreach (var window in _windows)
                {
                    window.Register(); //will set the GraphicsDevice for the first window
                }

                throw new DeviceChangedException();
            }
        }

        public static void SetGlobalFontScale(float scale)
        {
            _globalFontScale = scale;

            foreach (var window in _windows)
            {
                window.ImGuiIO.FontGlobalScale = _globalFontScale;
            }
                
        }

        public static void SetDefaultFont(string pathToTTF, float size=16, bool oversample = true)
        {
            _defaultFont_filePath = pathToTTF;
            _defaultFont_size = size;
            _defaultFont_isOversample = oversample;
        }

        internal static void Register(VeldridSDLWindow window, Sdl2Window sdl_window, GraphicsDeviceOptions options,
            out Swapchain swapchain, out ImGuiRenderer imguiRenderer, out ImFontPtr? mainFont,
            out ImGuiIOPtr imGuiIO)
        {
            if (GraphicsAPI.GD == null)
            {
                if (_requestedBackend == null)
                    _requestedBackend = GraphicsBackend.OpenGL;

                GraphicsAPI.SetGraphicsDevice(VeldridStartup.CreateGraphicsDevice(sdl_window, options, _requestedBackend.Value));

                swapchain = GraphicsAPI.GD!.MainSwapchain;
            }
            else
            {
                SwapchainDescription scDesc = new SwapchainDescription(
                VeldridStartup.GetSwapchainSource(sdl_window),
                window.Width,
                window.Height,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                false); //colorSrgb

                swapchain = GraphicsAPI.ResourceFactory!.CreateSwapchain(scDesc);
            }




            #region imgui
            imguiRenderer = new ImGuiRenderer(GraphicsAPI.GD, swapchain.Framebuffer.OutputDescription,
                (int)swapchain.Framebuffer.Width, (int)swapchain.Framebuffer.Height);


            imGuiIO = ImGui.GetIO();

            imGuiIO.FontGlobalScale = _globalFontScale;

            if (_defaultFont_filePath!=null)
            {
                unsafe
                {
                    var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();

                    //Add a higher horizontal/vertical sample rate for global scaling.
                    if (_defaultFont_isOversample)
                    {
                        (*nativeConfig).OversampleH = 8;
                        (*nativeConfig).OversampleV = 8;
                    }

                    (*nativeConfig).RasterizerMultiply = 1f;
                    (*nativeConfig).GlyphOffset = new Vector2(0);

                    mainFont = imGuiIO.Fonts.AddFontFromFileTTF(_defaultFont_filePath, _defaultFont_size, nativeConfig);
                }
            }
            else
            {
                mainFont = null;
            }
            

            #endregion

            imguiRenderer.CreateDeviceResources(GraphicsAPI.GD, swapchain.Framebuffer.OutputDescription);

            if (!_windows.Contains(window))
                _windows.Add(window);
        }

        internal static void Unregister(VeldridSDLWindow window)
        {
            bool valid = _windows.Remove(window);

            if (valid && _windows.Count == 0)
            {
                _running = false;
            }
        }

        internal static void Run()
        {
            if (_running)
                return;

            _running = true;

            while (_running)
            {
                Input.InputTracker.BeforeWindowFrameInputs();

                for (int i = 0; i < _windows.Count; i++)
                {

                    _windows[i].OnUpdate();
                }

                Input.InputTracker.AfterWindowFrameInputs();
            }

            GraphicsAPI.GD!.WaitForIdle();

            GraphicsAPI.SetGraphicsDevice(null); //Disposes the GraphicsDevice
        }
    }
}
