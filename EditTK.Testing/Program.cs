using EditTK.Windowing;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.ImGui;
using Silk.NET.WebGPU.Safe;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace EditTK.Testing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Environment.SetEnvironmentVariable("RUST_BACKTRACE", "full");

            Console.WriteLine("Initializing Graphics API");
            var webGPU = WebGPU.GetApi();

            var instance = webGPU.CreateInstance();

            var adapterTask = instance.RequestAdapter(backendType: default, powerPreference: PowerPreference.HighPerformance);

            adapterTask.Wait();

            var adapter = adapterTask.Result;

            var device = await adapter.RequestDevice(requiredLimits: null, requiredFeatures: null,
                deviceLostCallback: (r, m) =>
                {
                    var message = m?
                    .Replace("\\r\\n", "\n")
                    .Replace("\\n", "\n")
                    .Replace("\\t", "\t");
                    Debugger.Break();
                },
                defaultQueueLabel: "DefaultQueue", label: "MainDevice");

            device.SetUncapturedErrorCallback((type, m) =>
            {
                var message = m?
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
                Debugger.Break();
            });

            TestWindow.Create(new Vector2D<int>(800, 600), instance, adapter, device);

            WindowManager.Run();
        }
    }
}