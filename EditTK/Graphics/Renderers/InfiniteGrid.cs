using Silk.NET.Maths;
using Silk.NET.WebGPU;
using static Silk.NET.WebGPU.Safe.BindGroupEntries;
using static Silk.NET.WebGPU.Safe.BindGroupLayoutEntries;
using Silk.NET.WebGPU.Safe;
using Safe = Silk.NET.WebGPU.Safe;
using System.Diagnostics;
using EditTK.Utils;
using System.Numerics;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EditTK.Graphics.Renderers
{
    public static partial class InfiniteGrid
    {
        private struct Uniforms
        {
            public Matrix4X4<float> Transform;
            public Matrix4X4<float> ViewProjection;
            public Matrix2X4<float> TextureTransform;
            public Vector4D<float> GridColor;
            public Vector4D<float> Axis0Color;
            public Vector4D<float> Axis1Color;
        }

        public struct Vertex
        {
            [VertexAttribute(AttributeShaderLoc.Loc0, VertexFormat.Float32x3)]
            public Vector3D<float> Position;
            [VertexAttribute(AttributeShaderLoc.Loc1, VertexFormat.Float32x2)]
            public Vector2D<float> TexCoord;
            [VertexAttribute(AttributeShaderLoc.Loc2, VertexFormat.Float32x2)]
            public float Alpha;
        }

        private static bool s_isInitialized = false;
        private static RenderableMesh? s_FadingCircleMesh;
        private static BufferRange s_uniformBuffer;
        private static BindGroupPtr s_bindGroup;
        private static RecompilableRenderPipeline? s_RenderPipeline;
        private static RecompilableShaderModule? s_shaderModule;
        public const TextureFormat OutputFormat = TextureFormat.Rgba8UnormSrgb;

        public enum DefaultMesh
        {
            FadingCircle
        }

        private static RenderableMesh GetDefaultMesh(DefaultMesh key) => key switch
        {
            DefaultMesh.FadingCircle => s_FadingCircleMesh!,
            _ => throw new ArgumentException($"Invalid enum value {key} for DefaultMesh")
        };

        private static RenderableMesh GenerateFadingCircleMesh(DevicePtr device)
        {
            var mb = new ModelBuilder<ushort, Vertex>();

            var middle = new Vertex { Position = Vector3D<float>.Zero, TexCoord = Vector2D<float>.Zero, Alpha = 1 };

            var previous = new Vertex { Position = new(0, 0, 1), TexCoord = new(0, 1), Alpha = 0 };

            const int vertexCount = 16;

            for (int i = 1; i <= vertexCount; i++)
            {
                var (s, c) = MathF.SinCos(i * MathF.Tau / vertexCount);
                var current = new Vertex { Position = new(s, 0, c), TexCoord = new(s, c), Alpha = 0 };
                mb.AddTriangle(middle, current, previous);
                previous = current;
            }

            return mb.GetModel(device);
        }

        public unsafe static void EnsureInitialized(DevicePtr device)
        {
            if (s_isInitialized) return;
            s_isInitialized = true;

            s_FadingCircleMesh = GenerateFadingCircleMesh(device);

            bool ShaderFileUpdateCallback(byte[] data, out string? errorString)
            {
                return 
                    s_shaderModule!.TryUpdateAndRecompile(device, data, out errorString) &&
                    s_RenderPipeline!.TryUpdateAndRecompile(device, s_shaderModule, s_shaderModule, out errorString);

            }

            using (var stream = ResourceManager.GetFileFromExeFolder(new string[] { "res", "InfiniteGrid.wgsl" }, 
                ShaderFileUpdateCallback, out _))
            {
                Debug.Assert(stream != null);
                var ms = new MemoryStream();
                stream.CopyTo(ms);

                s_shaderModule = RecompilableShaderModule.CreateAndCompile(device, ms.GetBuffer().AsSpan(0..(int)ms.Length), 
                    Array.Empty<Safe.ShaderModuleCompilationHint>());
            }

            var bindGroupLayout = device.CreateBindGroupLayout(
                new ReadOnlySpan<BindGroupLayoutEntry>(
                    Buffer(0, ShaderStage.Vertex | ShaderStage.Fragment, BufferBindingType.Uniform, (ulong)sizeof(Uniforms))
                ));

            s_uniformBuffer = BufferHelper.CreateBufferWithData(device, BufferUsage.Uniform | BufferUsage.CopyDst, new Uniforms
            {
                ViewProjection = Matrix4X4<float>.Identity,
                Transform = Matrix4X4<float>.Identity,
            });

            s_bindGroup = device.CreateBindGroup(bindGroupLayout,
                new ReadOnlySpan<BindGroupEntry>(
                    Buffer(0, s_uniformBuffer.Buffer, s_uniformBuffer.Offset, s_uniformBuffer.Size)
                ));

            var layout = device.CreatePipelineLayout(
                new ReadOnlySpan<BindGroupLayoutPtr>(bindGroupLayout)
            );



            s_RenderPipeline = RecompilableRenderPipeline.CreateAndCompile(device, new Safe.RenderPipelineDescriptor(
                layout: layout,
                vertex: s_FadingCircleMesh.CreateVertexState(
                    ("vs_main", s_shaderModule.GetCompiled()),
                    Array.Empty<(string, double)>()
                ),
                primitive: new Safe.PrimitiveState
                {
                    CullMode = CullMode.None,
                    FrontFace = FrontFace.Ccw,
                    Topology = PrimitiveTopology.TriangleList
                },
                depthStencil: new DepthStencilState
                {
                    DepthCompare = CompareFunction.LessEqual,
                    Format = TextureFormat.Depth24PlusStencil8,
                    StencilFront = new StencilFaceState(CompareFunction.Always),
                    StencilBack = new StencilFaceState(CompareFunction.Always),
                },
                multisample: new MultisampleState { Count = 1, Mask = uint.MaxValue },
                fragment: new Safe.FragmentState
                {
                    Constants = Array.Empty<(string, double)>(),
                    EntryPoint = "fs_main",
                    Module = s_shaderModule.GetCompiled(),
                    Targets = new Safe.ColorTargetState[]
                    {
                    new Safe.ColorTargetState
                    {
                        Format = OutputFormat,
                        BlendState = (
                            color: new BlendComponent(BlendOperation.Add, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha),
                            alpha: new BlendComponent(BlendOperation.Add, BlendFactor.OneMinusDstAlpha, BlendFactor.One)
                        ),
                        WriteMask = ColorWriteMask.All
                    }
                    }
                }
            ));
        }

        public static void Draw(DevicePtr device, (TextureViewPtr color, TextureViewPtr depth) targets,
            in Matrix4X4<float> viewProjection,
            in Vector3 cameraPosition,
            (Vector4D<float> color, Vector3 direction) axis0,
            (Vector4D<float> color, Vector3 direction) axis1,
            in Vector4D<float> gridColor,
            Color? renderPassClearColor = null, (float depth, uint stencil)? renderPassClearDepthStencilValue = null)
        {
            var axisA = axis0.direction;
            var axisB = axis1.direction;

            var cross = Vector3.Normalize(Vector3.Cross(axisA, axisB));

            var plane = (normal: cross, origin: Vector3.Zero);

            var orientation = new Matrix4X4<float>(
            axisA.X, axisA.Y, axisA.Z, 0,
            cross.X, cross.Y, cross.Z, 0,
            axisB.X, axisB.Y, axisB.Z, 0,
            0, 0, 0, 1
            );

            var gridTransform = orientation * Matrix4X4.CreateScale(1000f) *
                Matrix4X4.CreateTranslation(MathUtils.ProjectOnPlane(cameraPosition, plane).ToGeneric());

            Debug.Assert(Matrix4X4.Invert(orientation, out var invertedOrientation));

            //remap world to plane uv:
            var m = gridTransform * invertedOrientation;
            var uvTransform = new Matrix3X2<float>(
                m.M11, m.M13, //x-axis direction.xz -> x-axis direction
                m.M31, m.M33, //z-axis direction.xz -> y-axis direction
                m.M41, m.M43  //translation.xz      -> translation
            );

            DrawAdvanced(device, targets, DefaultMesh.FadingCircle, gridTransform, viewProjection, uvTransform, gridColor,
                axis0.color, axis1.color, renderPassClearColor, renderPassClearDepthStencilValue);
        }

        public static void DrawAdvanced(DevicePtr device, (TextureViewPtr color, TextureViewPtr depth) targets, 
            Union<RenderableMesh, DefaultMesh> mesh,
            in Matrix4X4<float> transform, in Matrix4X4<float> viewProjection, in Matrix3X2<float> textureTransform,
            in Vector4D<float> gridColor, in Vector4D<float> axis0Color, in Vector4D<float> axis1Color,
            Color? renderPassClearColor = null, (float depth, uint stencil)? renderPassClearDepthStencilValue = null)
        {
            EnsureInitialized(device);

            var queue = device.GetQueue();
            queue.WriteBuffer(s_uniformBuffer.Buffer, s_uniformBuffer.Offset,
                new ReadOnlySpan<Uniforms>(new Uniforms
                {
                    Transform = transform,
                    ViewProjection = viewProjection,
                    TextureTransform = new Matrix2X4<float>(
                        textureTransform.M11, textureTransform.M21, textureTransform.M31, 0,
                        textureTransform.M12, textureTransform.M22, textureTransform.M32, 0
                    ),
                    GridColor = gridColor,
                    Axis0Color = axis0Color,
                    Axis1Color = axis1Color
                }));

            var cmd = device.CreateCommandEncoder();

            var pass = cmd.BeginRenderPass(
                new ReadOnlySpan<Safe.RenderPassColorAttachment>(
                    new Safe.RenderPassColorAttachment
                    {
                        ClearValue = renderPassClearColor.GetValueOrDefault(),
                        LoadOp = renderPassClearColor.HasValue ? LoadOp.Clear : LoadOp.Load,
                        StoreOp = StoreOp.Store,
                        View = targets.color
                    }
                ), null, 
                new Safe.RenderPassDepthStencilAttachment
                {
                    DepthClearValue = renderPassClearDepthStencilValue.GetValueOrDefault().depth,
                    StencilClearValue = renderPassClearDepthStencilValue.GetValueOrDefault().stencil,
                    DepthLoadOp = renderPassClearDepthStencilValue.HasValue ? LoadOp.Clear : LoadOp.Load, 
                    DepthStoreOp = StoreOp.Store, DepthReadOnly = renderPassClearDepthStencilValue is null,
                    StencilLoadOp = renderPassClearDepthStencilValue.HasValue ? LoadOp.Clear : LoadOp.Load, 
                    StencilStoreOp = StoreOp.Store, StencilReadOnly = renderPassClearDepthStencilValue is null,
                    View = targets.depth
                }, null
            );

            pass.SetPipeline(s_RenderPipeline!.GetCompiled());
            pass.SetBindGroup(0, s_bindGroup, null);

            if(mesh.TryGetT1(out var renderableMesh))
                renderableMesh!.Draw(pass);
            else if(mesh.TryGetT2(out var defaultMesh))
            {
                renderableMesh = GetDefaultMesh(defaultMesh);

                renderableMesh.Draw(pass);
            }

            pass.End();

            queue.Submit(new ReadOnlySpan<CommandBufferPtr>(cmd.Finish()));
        }
    }
}
