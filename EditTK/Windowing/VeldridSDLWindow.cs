using System;
using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;
using EditTK.Input;
using System.Runtime.CompilerServices;
using System.Numerics;
using ImGuiNET;
using EditTK.Core.Timing;

using static EditTK.Graphics.GraphicsAPI;
using EditTK.Graphics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace EditTK.Windowing
{
    /// <summary>
    /// Provides the base of an SDL window that is part of Veldrids and EditTKs ecosystem
    /// </summary>
    public unsafe abstract class VeldridSDLWindow
    {
        #region WINAPI
        private struct PaintStruct
        {
            public IntPtr hdc;
            public bool fErase;
            public Rectangle rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            
            //BYTE rgbReserved[32];

            public long rgbReserved0;
            public long rgbReserved1;
            public long rgbReserved2;
            public long rgbReserved3;
        }

        private static uint BLACK_BRUSH = 4;

        private static uint ETO_OPAQUE = 0x0002;


        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern bool DwmSetWindowAttribute(IntPtr handle, int param, in int value, int size);


        [DllImport("uxtheme.dll", SetLastError = true)]
        private static extern bool SetWindowTheme(IntPtr handle, string? subAppName, string? subIDList);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr BeginPaint(IntPtr handle, out PaintStruct ps);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void EndPaint(IntPtr handle, in PaintStruct ps);


        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SetBkColor(IntPtr hdc, uint color);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr ExtTextOutA(IntPtr hdc, int x, int y, uint options, in Rectangle lprect, string? lpString, uint c, in int lpDx);
        #endregion

        private class OverrideableInputSnapshot : InputSnapshot
        {
            InputSnapshot _baseSnapshot;

            Vector2? _mousePosOverride;
            bool[]? _mouseButtonsOverride;

            public OverrideableInputSnapshot(InputSnapshot baseSnapshot, Vector2? mousePosOverride, bool[]? mouseButtonsOverride)
            {
                _baseSnapshot = baseSnapshot;
                _mousePosOverride = mousePosOverride;
                _mouseButtonsOverride = mouseButtonsOverride;
            }

            public IReadOnlyList<KeyEvent> KeyEvents => _baseSnapshot.KeyEvents;

            public IReadOnlyList<MouseEvent> MouseEvents => _baseSnapshot.MouseEvents;

            public IReadOnlyList<char> KeyCharPresses => _baseSnapshot.KeyCharPresses;

            public Vector2 MousePosition => _mousePosOverride ?? _baseSnapshot.MousePosition;

            public float WheelDelta => _baseSnapshot.WheelDelta;

            public bool IsMouseDown(MouseButton button)
            {
                return _mouseButtonsOverride?[(int)button] ?? _baseSnapshot.IsMouseDown(button);
            }
        }

        public GlobalTimeTracker TimeTracker { get; private set; }

        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain MainSwapchain { get; private set; }
        public Fence CommandListFence { get; private set; }
        public OutputDescription OutputDescription { get; private set; } = 
            new OutputDescription(null, new OutputAttachmentDescription(PixelFormat.B8_G8_R8_A8_UNorm));
        public ImGuiRenderer ImGuiRenderer { get; private set; }
        protected ImFontPtr? MainFont { get; private set; }
        public ImGuiIOPtr ImGuiIO { get; private set; }
        public ImFontAtlasPtr Fonts { get; private set; }

        private IntPtr _imguiContext;


        private Sdl2Window _window;
        private WindowCreateInfo _wci;
        private bool _windowResized = true;
        
        private Stopwatch _stopWatch;
        private double _previousElapsed;
        private CommandList _cl;

        public uint Width => (uint)(_window?.Width ?? _wci.WindowWidth);
        public uint Height => (uint)(_window?.Height ?? _wci.WindowHeight);

        public bool Focused => (_window?.Focused ?? false);


        public string Title
        {
            get => _window?.Title ?? _wci.WindowTitle;
            set
            {
                if (_window == null)
                    _wci.WindowTitle = value;
                else
                    _window.Title = value;
            }
        }

        public bool Hovered { get; private set; }

        private SDL_WindowFlags _additionalFlags;

        public VeldridSDLWindow(WindowCreateInfo wci, SDL_WindowFlags additionalFlags = default)
        {
            _wci = wci;
            _additionalFlags = additionalFlags;

            TimeTracker = new GlobalTimeTracker();
            
        }

        private void CreateSDLWindow()
        {
            _window?.Close();

            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable
                    | GetWindowFlags(_wci.WindowInitialState) | _additionalFlags |SDL_WindowFlags.Hidden;

            _window = new Sdl2Window(
                _wci.WindowTitle,
                _wci.X,
                _wci.Y,
                _wci.WindowWidth,
                _wci.WindowHeight,
                flags,
                false);

            _window.Resized += () =>
            {
                _windowResized = true;
            };

            _window.MouseEntered += () => Hovered = true;
            _window.MouseLeft    += () => Hovered = false;

            _window.KeyDown += OnKeyDown;


            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //Support dark mode on Windows
                //ported from https://github.com/libsdl-org/SDL/issues/4776#issuecomment-926976455

                SetWindowTheme(_window.Handle, "DarkMode_Explorer", null);

                if (!DwmSetWindowAttribute(_window.Handle, 20, 1, sizeof(int)))
                    DwmSetWindowAttribute(_window.Handle, 19, 1, sizeof(int));


                _window.Opacity = 0;

                if (_wci.WindowInitialState != WindowState.Hidden)
                    Sdl2Native.SDL_ShowWindow(_window.SdlWindowHandle);



                IntPtr hdc = BeginPaint(_window.Handle, out PaintStruct ps);
                SetBkColor(hdc, BLACK_BRUSH);

                ExtTextOutA(hdc, 0, 0, ETO_OPAQUE, new Rectangle(0, 0, (int)Width, (int)Height), null, 0, 0);
                EndPaint(_window.Handle, ps);

                Thread.Sleep(100);

                _window.Opacity = 100;
            }
            else
            {
                if (_wci.WindowInitialState != WindowState.Hidden)
                    Sdl2Native.SDL_ShowWindow(_window.SdlWindowHandle);
            }

            Console.WriteLine(Sdl2Native.SDL_GetWindowFlags(_window.SdlWindowHandle));
        }

        private static SDL_WindowFlags GetWindowFlags(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;
                case WindowState.FullScreen:
                    return SDL_WindowFlags.Fullscreen;
                case WindowState.Maximized:
                    return SDL_WindowFlags.Maximized;
                case WindowState.Minimized:
                    return SDL_WindowFlags.Minimized;
                case WindowState.BorderlessFullScreen:
                    return SDL_WindowFlags.FullScreenDesktop;
                case WindowState.Hidden:
                    return SDL_WindowFlags.Hidden;
                default:
                    throw new VeldridException("Invalid WindowState: " + state);
            }
        }

        internal void Register()
        {
            if(_window != null)
            {
                _wci.X = _window.X;
                _wci.Y = _window.Y;
                _wci.WindowWidth = _window.Width;
                _wci.WindowHeight = _window.Height;
            }

            CreateSDLWindow();

            GraphicsDeviceOptions options = new(
                debug: false,
                swapchainDepthFormat: null,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);
#if DEBUG
            options.Debug = true;
#endif

            WindowManager.Register(this, _window, options,

                out Swapchain swapchain, out ImGuiRenderer imGuiRenderer, out ImFontPtr? imFontPtr,
                out ImGuiIOPtr imGuiIO);

            MainFont = imFontPtr;
            ImGuiIO = imGuiIO;
            Fonts = imGuiIO.Fonts;
            ImGuiRenderer = imGuiRenderer;
            _imguiContext = ImGui.GetCurrentContext();

            OnGraphicsDeviceCreated(GraphicsAPI.ResourceFactory!, swapchain);

            HandleWindowResize();
        }

        public void Run(Action? callBack = null)
        {
            Register();
            
            _stopWatch = Stopwatch.StartNew();
            _previousElapsed = _stopWatch.Elapsed.TotalSeconds;


            callBack?.Invoke();

            WindowManager.Run();
        }

        internal void OnUpdate()
        {
            double newElapsed = _stopWatch.Elapsed.TotalSeconds;
            float deltaSeconds = (float)(newElapsed - _previousElapsed);

            InputSnapshot inputSnapshot = _window.PumpEvents();

            InputTracker.UpdateWindowFrameInput(inputSnapshot, _window, Hovered);

            Debug.Assert(ImGuiRenderer != null);


            var buttons = new bool[3];

            buttons[0] = InputTracker.GetMouseButton(MouseButton.Left);
            buttons[1] = InputTracker.GetMouseButton(MouseButton.Middle);
            buttons[2] = InputTracker.GetMouseButton(MouseButton.Right);

            ImGui.SetCurrentContext(_imguiContext);
            ImGuiRenderer.Update(deltaSeconds, new OverrideableInputSnapshot(
                InputTracker.FrameSnapshot!, InputTracker.MousePosition, buttons));




            ImGui.GetIO().MousePos = InputTracker.MousePosition;



            TimeTracker.Update(deltaSeconds);

            if (_window.Exists)
            {
                try
                {
                    _previousElapsed = newElapsed;
                    if (_windowResized)
                    {
                        _windowResized = false;
                        MainSwapchain!.Resize((uint)_window.Width, (uint)_window.Height);
                        ImGuiRenderer.WindowResized(_window.Width, _window.Height);
                        HandleWindowResize();
                    }

                    CommandListFence.Reset();

                    _cl.Begin();

                    _cl.InsertDebugMarker("Frame Start - " + _window.Title);

                    ImGui.PushFont(MainFont ?? null);
                    _cl.SetFramebuffer(MainSwapchain.Framebuffer);
                    _cl.ClearColorTarget(0, RgbaFloat.Black);

                    Draw(deltaSeconds, _cl);

                    ImGuiRenderer.Render(GD, _cl);

                    _cl.End();
                    GD.SubmitCommands(_cl, CommandListFence);
                    GD.SwapBuffers(MainSwapchain);
                }
                catch (DeviceChangedException)
                {

                }
                
            }
            else
            {
                WindowManager.Unregister(this);
            }
        }

        public void OnGraphicsDeviceCreated(ResourceFactory factory, Swapchain sc)
        {
            ResourceFactory = factory;
            MainSwapchain = sc;

            CommandListFence = factory.CreateFence(false);

            CreateResources(factory);
            CreateSwapchainResources(factory);

            _cl = factory.CreateCommandList();
        }

        protected virtual void OnDeviceDestroyed()
        {
            ResourceFactory = null;
            MainSwapchain = null;
        }

        protected abstract void CreateResources(ResourceFactory factory);

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        protected abstract void Draw(float deltaSeconds, CommandList cl);

        protected virtual void HandleWindowResize()
        {

        }

        protected virtual void OnKeyDown(KeyEvent ke) { }
    }
}
