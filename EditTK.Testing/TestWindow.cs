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
using EditTK.Graphics.Framebuffers;
using EditTK.Graphics.Renderers;
using System.Numerics;
using Silk.NET.Input;
using EditTK.Interactions;
using EditTK.Utils;
using System.Diagnostics;

namespace EditTK.Testing
{
    internal sealed class TestWindow
    {
        public static TestWindow Create(Vector2D<int> size, InstancePtr instance, AdapterPtr adapter, DevicePtr device)
        {
            var testWindow = new TestWindow(instance, adapter, device);

            WindowManager.CreateWindow(size, instance, adapter, device, testWindow.Render, out IWindow window);

            testWindow._window = window;
            window.Load += () =>
            {
                testWindow._input = window.CreateInput();
            };

            return testWindow;
        }

        private Vector4 _gridColor = new(.125f, .125f, .125f, .5f);
        private Vector4 _axisColorX = new(1f, 0.1f, 0.1f, 1f);
        private Vector4 _axisColorY = new(0.1f, 0.3f, 1f, 1f);
        private Vector4 _axisColorZ = new(0.1f, 1f, 0.1f, 1f);

        private IWindow? _window;
        private InstancePtr _instance;
        private AdapterPtr _adapter;
        private DevicePtr _device;

        private RenderTexture _renderTexture;

        private TextureViewPtr? _prevSceneColorTexView = null;

        private readonly FlyCamera _camera = new(new(0, 1, 15), Vector3.Zero);
        private IInputContext? _input;

        private TestWindow(InstancePtr instance, AdapterPtr adapter, DevicePtr device)
        {
            _instance = instance;
            _adapter = adapter;
            _device = device;

            _renderTexture = RenderTexture.Create(device, InfiniteGrid.OutputFormat, TextureFormat.Depth24PlusStencil8);
        }

        private void Render(double deltaSeconds, TextureViewPtr swapchainView, ImGuiController imguiController)
        {
            var queue = _device.GetQueue();
            var cmd = _device.CreateCommandEncoder();

            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowViewport(viewport.ID);
            ImGui.SetNextWindowPos(new Vector2(0,0));
            ImGui.SetNextWindowSize(viewport.Size);

            ImGui.PushStyleColor(ImGuiCol.Border, 0);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFF_05_05_05);
            ImGui.Begin("Test", ImGuiWindowFlags.NoDecoration);
            ImGui.PopStyleColor(2);

            ImGui.Columns(2);
            ImGui.Text($"Framerate: {ImGui.GetIO().Framerate}");
            if (ImGui.Button("Add Window"))
                Create(new(800, 600), _instance, _adapter, _device);

            ImGui.ColorEdit4("GridColor", ref _gridColor);
            ImGui.ColorEdit4("X-Axis Color", ref _axisColorX);
            ImGui.ColorEdit4("Y-Axis Color", ref _axisColorY);
            ImGui.ColorEdit4("Z-Axis Color", ref _axisColorZ);

            ImGui.NextColumn();

            if (ImGui.Button("Look at Origin"))
                _camera.LookAt(_camera.Eye, Vector3.Zero);

            ImGui.Spacing();

            if (ImGui.Button("Set X as Up"))
                _camera.TurntableUpVector = Vector3.UnitX;
            ImGui.SameLine();
            if (ImGui.Button("Set Y as Up"))
                _camera.TurntableUpVector = Vector3.UnitY;
            ImGui.SameLine();
            if (ImGui.Button("Set Z as Up"))
                _camera.TurntableUpVector = Vector3.UnitZ;
            ImGui.SameLine();
            if (ImGui.Button("Set no up"))
                _camera.TurntableUpVector = null;

            ImGui.DragFloat("Smoothness", ref _camera.Smoothness);

            ImGui.Columns();

            var size = ImGui.GetContentRegionAvail();
            _renderTexture.EnsureSize((uint)size.X, (uint)size.Y);

            if (_renderTexture.TryGetTargets(out var sceneColor, out var sceneDepthStencil))
            {
                if (_prevSceneColorTexView != sceneColor.View && _prevSceneColorTexView is not null)
                    imguiController.RemoveTextureBindGroup(_prevSceneColorTexView.Value);

                _prevSceneColorTexView = sceneColor.View;

                var aspectRatio = size.X / size.Y;

                _camera.Update((float)deltaSeconds, _input!, true);

                var viewProjectionMatrix = _camera.GetViewProjection(aspectRatio);

                InfiniteGrid.Draw(_device, (sceneColor.View, sceneDepthStencil!.Value.View), 
                    viewProjectionMatrix.ToGeneric(), _camera.Eye,
                    (_axisColorX.ToGeneric(), Vector3.UnitX), (_axisColorZ.ToGeneric(), Vector3.UnitZ),
                    _gridColor.ToGeneric(),
                    renderPassClearColor: new Color(0.01f, 0.01f, 0.01f, 1f), 
                    renderPassClearDepthStencilValue: (1f, 0));

                var camForward = Vector3.Transform(Vector3.UnitZ, _camera.Rotation);

                var cross = Vector3.Normalize(Vector3.Cross(camForward, Vector3.UnitY));

                InfiniteGrid.Draw(_device, (sceneColor.View, sceneDepthStencil!.Value.View),
                    viewProjectionMatrix.ToGeneric(), _camera.Eye,
                    (default, cross), (_axisColorY.ToGeneric(), Vector3.UnitY),
                    default);


                ImGui.Image(sceneColor.View.GetIntPtr(), size);
            }

            ImGui.End();

            ImGui.ShowDemoWindow();

            {
                var pass = cmd.BeginRenderPass(new Safe.RenderPassColorAttachment[]
                {
                    new(swapchainView, resolveTarget: null,
                    LoadOp.Clear, StoreOp.Store, new Color(.1, .1, .1, 1))
                }, null, null, null);

                imguiController!.Render(pass);

                pass.End();
            }

            queue.Submit(new[] { cmd.Finish() });
        }
    }
}
