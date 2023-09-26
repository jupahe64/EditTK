using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Safe;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Safe = Silk.NET.WebGPU.Safe;

namespace EditTK.Graphics
{
    public partial class RecompilableRenderPipeline
    {
        private Safe.RenderPipelineDescriptor _descriptor;
        private List<RenderPipelinePtr> _invalidatedResources = new();
        internal RenderPipelinePtr CompiledInternal { get; private set; }

        private RecompilableRenderPipeline(Safe.RenderPipelineDescriptor descriptor, RenderPipelinePtr compiled)
        {
            _descriptor = descriptor;
            CompiledInternal = compiled;
        }

        /// <summary>
        /// Get's the latest compiled pipeline and invalidates all previous versions
        /// <para>To avoid race conditions this method should only be called directly before rendering</para>
        /// </summary>
        /// <returns></returns>
        public RenderPipelinePtr GetCompiled()
        {
            lock (_invalidatedResources)
            {
                for (int i = 0; i < _invalidatedResources.Count; i++)
                    _invalidatedResources[i].Release();

                _invalidatedResources.Clear();
            }
            return CompiledInternal;
        }

        public static RecompilableRenderPipeline CreateAndCompile(DevicePtr device, Safe.RenderPipelineDescriptor descriptor)
        {
            var pipeline = device.CreateRenderPipeline(descriptor);

            return new(descriptor, pipeline);
        }

        public bool TryUpdateAndRecompile(DevicePtr device,
            RecompilableShaderModule? newVertexShaderModule,
            RecompilableShaderModule? newFragmentShaderModule,
            [MaybeNullWhen(false)] out string? errorString)
        {
            return TryUpdateAndRecompile(device, 
                newVertexShaderModule?.CompiledInternal, newFragmentShaderModule?.CompiledInternal, out errorString);
        }

        public bool TryUpdateAndRecompile(DevicePtr device, 
            ShaderModulePtr? newVertexShaderModule,
            ShaderModulePtr? newFragmentShaderModule, 
            [MaybeNullWhen(false)] out string? errorString)
        {
            if (newVertexShaderModule.HasValue)
                _descriptor.Vertex.Module = newVertexShaderModule.Value;
            if (newFragmentShaderModule.HasValue)
                _descriptor.Fragment = _descriptor.Fragment!.Value with { Module = newFragmentShaderModule.Value };

            device.PushErrorScope(ErrorFilter.Validation);
            var recompiledPipeline = device.CreateRenderPipeline(_descriptor);
            Task<GPUError?> errorTask = device.PopErrorScope();
            errorTask.Wait();

            if(errorTask.Result == null)
            {
                lock (_invalidatedResources)
                    _invalidatedResources.Add(CompiledInternal);

                CompiledInternal = recompiledPipeline;
                errorString = null;
                return true;
            }

            recompiledPipeline.Release();

            var message = errorTask.Result!.Message;
            var m = WgpuValidationErrorRegex().Match(message);
            Debug.Assert(m.Success);

            var reason = m.Groups[1].Value;
            errorString = RustEnumRegex().Replace(reason, x => $"'{x.Groups[1].Value}'");

            return false;
        }

        [GeneratedRegex("Validation Error\n\nCaused by:\n    In (?:.*)\n((.|\n)*)", 
            RegexOptions.Multiline)]
        private static partial Regex WgpuValidationErrorRegex();

        [GeneratedRegex("\\w*\\((\\w*)\\)")]
        private static partial Regex RustEnumRegex();
    }

    public partial class RecompilableShaderModule
    {
        private (Safe.ShaderModuleCompilationHint[] hints, string? label) _descriptorExtras;
        private List<ShaderModulePtr> _invalidatedResources = new();
        internal ShaderModulePtr CompiledInternal { get; private set; }

        private RecompilableShaderModule((Safe.ShaderModuleCompilationHint[] hints, string? label) descriptorExtras, 
            ShaderModulePtr compiled)
        {
            _descriptorExtras = descriptorExtras;
            CompiledInternal = compiled;
        }

        /// <summary>
        /// Get's the latest compiled module and invalidates all previous versions
        /// <para>To avoid race conditions this method should only be called directly before rendering</para>
        /// </summary>
        /// <returns></returns>
        public ShaderModulePtr GetCompiled()
        {
            lock (_invalidatedResources)
            {
                for (int i = 0; i < _invalidatedResources.Count; i++)
                    _invalidatedResources[i].Release();

                _invalidatedResources.Clear();
            }
            return CompiledInternal;
        }

        public static RecompilableShaderModule CreateAndCompile(DevicePtr device, ReadOnlySpan<byte> code,
            Safe.ShaderModuleCompilationHint[] hints, string? label = null, bool isSpirv = false)
        {
            var module = isSpirv ?
                device.CreateShaderModuleSPIRV(code, hints, label) :
                device.CreateShaderModuleWGSL(code, hints, label);

            var descriptorExtras = (hints,  label);

            return new(descriptorExtras, module);
        }

        public bool TryUpdateAndRecompile(DevicePtr device,
            ReadOnlySpan<byte> newCode,
            [MaybeNullWhen(false)] out string? errorString, bool isSpirv = false)
        {
            device.PushErrorScope(ErrorFilter.Validation);
            var recompiledModule = isSpirv ?
                device.CreateShaderModuleSPIRV(newCode, _descriptorExtras.hints, _descriptorExtras.label) :
                device.CreateShaderModuleWGSL(newCode, _descriptorExtras.hints, _descriptorExtras.label);
            Task<GPUError?> errorTask = device.PopErrorScope();
            errorTask.Wait();

            if (errorTask.Result == null)
            {
                lock (_invalidatedResources)
                    _invalidatedResources.Add(CompiledInternal);

                CompiledInternal = recompiledModule;
                errorString = null;
                return true;
            }

            recompiledModule.Release();

            var message = errorTask.Result!.Message;
            var m = WgpuValidationErrorRegex().Match(message);
            Debug.Assert(m.Success);

            var reason = m.Groups[1].Value;
            errorString = RustEnumRegex().Replace(reason, x => $"'{x.Groups[1].Value}'");

            return false;
        }

        [GeneratedRegex("Validation Error\n\nCaused by:\n    In (?:.*)\n    \n((.|\n)*)", 
            RegexOptions.Multiline)]
        private static partial Regex WgpuValidationErrorRegex();

        [GeneratedRegex("\\w*\\((\\w*)\\)")]
        private static partial Regex RustEnumRegex();
    }
}
