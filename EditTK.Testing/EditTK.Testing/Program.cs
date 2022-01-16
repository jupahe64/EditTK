using EditTK.Util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace EditTK.Testing
{
    class Program
    {
        public static float Dpi { get; private set; } = 1;

        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        static void Main(string[] args)
        {
            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //Will only work on Windows

                var window = new Sdl2Window("Test", 0, 0, 1, 1, SDL_WindowFlags.Hidden | SDL_WindowFlags.Fullscreen, false);

                float before = window.Width;
                window.Close();

                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);

                window = new Sdl2Window("Test", 0, 0, 1, 1, SDL_WindowFlags.Hidden | SDL_WindowFlags.Fullscreen, false);

                float after = window.Width;
                window.Close();

                Dpi = after/before;
            }

            WindowManager.RequestBackend(GraphicsBackend.OpenGL);

            WindowManager.SetGlobalFontScale(Dpi);

            WindowManager.SetDefaultFont(SystemUtils.RelativeFilePath("Resources", "Font.ttf"), 14);

            {
                WindowCreateInfo windowCI = new() {
                    X = 100,
                    Y = 100,
                    WindowWidth = (int) (960 * Dpi),
                    WindowHeight = (int) (540 * Dpi),
                    WindowTitle = "EditTK Playground"
                };

                var w = new TestWindow(windowCI);

                //w.CustomFlags = SDL_WindowFlags.Borderless;

                w.Run();
            }
        }
    }
}
