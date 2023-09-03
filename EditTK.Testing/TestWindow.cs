using EditTK.Windowing;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.ImGui;
using Silk.NET.WebGPU.Safe;
using Silk.NET.Windowing;
using Safe = Silk.NET.WebGPU.Safe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Testing
{
    internal sealed class TestWindow
    {
        private IWindow? _window;
        private InstancePtr _instance;
        private AdapterPtr _adapter;
        private DevicePtr _device;

        public TestWindow(InstancePtr instance, AdapterPtr adapter, DevicePtr device)
        {
            _instance = instance;
            _adapter = adapter;
            _device = device;
        }

        public static TestWindow Create(Vector2D<int> size, InstancePtr instance, AdapterPtr adapter, DevicePtr device)
        {
            var testWindow = new TestWindow(instance, adapter, device);

            WindowManager.CreateWindow(size, instance, adapter, device, testWindow.Render, out IWindow window);

            testWindow._window = window;

            return testWindow; 
        }

        private void Render(double deltaSeconds, TextureViewPtr swapchainView, ImGuiController imguiController)
        {
            var queue = _device.GetQueue();
            var cmd = _device.CreateCommandEncoder();

            ImGui.Begin("Test");
            ImGui.Text($"Framerate: {ImGui.GetIO().Framerate}");
            if (ImGui.Button("Add Window"))
                Create(new(800, 600), _instance, _adapter, _device);

            ImGui.End();

            ImGui.ShowDemoWindow();

            var pass = cmd.BeginRenderPass(new Safe.RenderPassColorAttachment[]
            {
                        new(swapchainView, resolveTarget: null,
                        LoadOp.Clear, StoreOp.Store, new Color(.1, .1, .1, 1))
            }, null, null, null);

            imguiController!.Render(pass);

            pass.End();

            queue.Submit(new[] { cmd.Finish() });
        }
    }
}
