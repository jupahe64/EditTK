using ImGuiNET;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using EditTK.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Veldrid.StartupUtilities;
using EditTK.Windowing;
using EditTK.Cameras;
using EditTK.UI;
using EditTK.Graphics;
using EditTK.Util;
using EditTK.Core.Transform;
using EditTK.Testing;
using Veldrid.ImageSharp;

using static EditTK.Graphics.GraphicsAPI;
using EditTK.Graphics.Helpers;
using EditTK.Graphics.Rendering;

namespace EditTK
{
    /// <summary>
    /// An old test window that got corrupted very fast (on some backends)
    /// <para>It had some really cool effects and functionality but I had to comment out almost all of it</para>
    /// <para>Now it's mainly a reference/copy source</para>
    /// <para>For the new test window see <see cref="TestWindow"/></para>
    /// </summary>
    class PlaygroundWindow : VeldridSDLWindow
    {
        struct SceneUB
        {
            public Matrix4x4 View;
            public Vector3 CamPlaneNormal;
            public float CamPlaneOffset;
            public Vector2 ViewportSize;
            public float ForceSolidHighlight;
            public float BlendAlpha;

            public SceneUB(Matrix4x4 view, Vector3 camPlaneNormal, float camPlaneOffset, Vector2 viewportSize, float forceSolidHighlight, float blendAlpha)
            {
                View = view;
                CamPlaneNormal = camPlaneNormal;
                CamPlaneOffset = camPlaneOffset;
                ViewportSize = viewportSize;
                ForceSolidHighlight = forceSolidHighlight;
                BlendAlpha = blendAlpha;
            }
        }

        private Camera _cam;

        private readonly SimpleCam _simpleCam = new SimpleCam();

        float _lastFps = 60;
        Vector3 _camTarget = Vector3.Zero;
        float _camYaw = 0;
        float _camPitch = 0.5f;

        SceneUB _ubSceneData = new(Matrix4x4.Identity, Vector3.Zero,0,Vector2.Zero,0,1);

        private DeviceBuffer? _ubScene;
        private DeviceBuffer? _ubHoverColer;
        private ShaderUniformLayout _sceneUniformLayout;
        private ResourceSet? _sceneResourceSet;
        private float _ticks;

        private GizmoDrawer? _gizmoDrawer;
        //private RotationGizmo _gizmo;
        private readonly GenericObjectRenderer<ushort, VeldridUtils.VertexPositionTexture> _cubeRenderer;
        private GenericObjectRenderer<ushort, VeldridUtils.VertexPositionTexture> _planeRenderer;
        private int _lastHoveredID;
        private int _currentHoveredID;
        private float _currentMouseDepth;

        private float _near = 0.01f;
        private float _far = 1000f;
        private float _fov = 1f;

        private Texture? _sceneDepth0Texture;
        private Texture _sceneDepth1Texture;
        private Texture? _sceneDepthStagingTexture;
        private Texture? _sceneColor0Texture;
        private Texture? _sceneColor1Texture;
        //private Texture? _scenePicking0Texture;
        private Texture _scenePickingCTexture;
        private Texture? _scenePickingStagingTexture;
        private Texture? _sceneDepthCopyTexture;
        private Texture _finalTexture;
        private IntPtr _finalTextureBinding;
        private Framebuffer? _sceneFramebuffer;
        private Framebuffer _sceneHighlightFramebuffer;
        private Framebuffer? _finalFramebuffer;
        private Framebuffer? _depthCopyFramebuffer;
        private GenericObjectRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _fullscreenQuadRenderer;
        private GenericObjectRenderer<ushort, VeldridUtils.VertexFullscreenQuad>? _depthCopyQuadRenderer;
        private ResourceSet[]? _fullscreenQuadParamsSets;
        private ResourceSet[]? _depthCopyQuadParamsSets;
        private float _lastCleanupTicks = 0;
        private Vector3 _currentCameraRay;
        private Vector2 _lastMousePos;
        private ImGuiIOPtr _io;
        private ImDrawListPtr _foregroundDrawlist;
        private ImDrawListPtr _drawlist;
        private bool _hasActiveAction;
        private float _dragMouseDepth = 0;
        private Vector3 _lastCameraPosition;
        //private SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture> _plane;
        //private SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture> _ghostObj;
        //private SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture> _bugfixObject;
        private readonly List<object> _objects = new();


        //private readonly ShaderParamID COLOR0_TEX_PARAM = new ("COLOR0_TEX_PARAM");
        //private readonly ShaderParamID COLOR1_TEX_PARAM = new ("COLOR1_TEX_PARAM");
        //private readonly ShaderParamID DEPTH0_TEX_PARAM = new ("DEPTH0_TEX_PARAM");
        //private readonly ShaderParamID DEPTH1_TEX_PARAM = new ("DEPTH1_TEX_PARAM");
        //private readonly ShaderParamID DEPTH2_TEX_PARAM = new ("DEPTH2_TEX_PARAM");

        //private readonly ShaderParamID PICKING1_TEX_PARAM = new("PICKING1_TEX_PARAM");

        //private readonly ShaderParamID TEX_SAMPLER_PARAM = new("TEX_SAMPLER_PARAM");
        //private readonly ShaderParamID PICKING_SAMPLER_PARAM = new("PICKING_SAMPLER_PARAM");

        //private readonly ShaderParamID HOVER_COLOR_PARAM = new("HOVER_COLOR_PARAM");

        private event Action<ResourceFactory> CreateDeviceResources;
        private event Action<ResourceFactory> CreateScreenResources;

        public PlaygroundWindow(WindowCreateInfo wci) : base(wci)
        {
            orientationCubeTexture = new ImageSharpTexture(SystemUtils.RelativeFilePath("Resources", "OrientationCubeTex.png"));

            #region Objects

            var cubeVertices = GetCubeVertices();
            var cubeIndices = GetCubeIndices();

            var planeVertices = new VeldridUtils.VertexPositionTexture[]
            {
                new VeldridUtils.VertexPositionTexture(new Vector3(-100, 0, -100), Vector2.Zero),
                new VeldridUtils.VertexPositionTexture(new Vector3( 100, 0, -100), Vector2.Zero),
                new VeldridUtils.VertexPositionTexture(new Vector3(-100, 0,  100), Vector2.Zero),
                new VeldridUtils.VertexPositionTexture(new Vector3( 100, 0,  100), Vector2.Zero),
            };

            var planeIndices = new ushort[]
            {
                0,1,2,1,3,2
            };

            //_gizmo = new RotationGizmo(Matrix4x4.Identity, false);


            _sceneUniformLayout = ShaderUniformLayoutBuilder.Get()
                .BeginUniformBuffer("ub_Scene", ShaderStages.Vertex | ShaderStages.Fragment)
                    .AddUniform<Matrix4x4>("View")
                    .AddUniform<Vector3>("CamPlaneNormal")
                    .AddUniform<float>("CamPlaneOffset")
                    .AddUniform<Vector2>("ViewportSize")
                    .AddUniform<float>("ForceSolidHighlight")
                    .AddUniform<float>("BlendAlpha")
                .EndUniformBuffer()
                .GetLayout();

            //_cubeRenderer = new GenericObjectRenderer<ushort, VeldridUtils.VertexPositionTexture>(cubeVertices, cubeIndices, Encoding.UTF8.GetBytes(Cube_VertexCode), Encoding.UTF8.GetBytes(Cube_FragmentCode),
            //    blendState: new BlendStateDescription(RgbaFloat.White, BlendAttachmentDescription.OverrideBlend, BlendAttachmentDescription.Disabled),
            //    rasterizerState: new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false)
            //    );

            _planeRenderer = new GenericObjectRenderer<ushort, VeldridUtils.VertexPositionTexture>(planeVertices, planeIndices, Encoding.UTF8.GetBytes(Plane_VertexCode), Encoding.UTF8.GetBytes(Plane_FragmentCode),
                unformSetLayouts: new ShaderUniformLayout[]
                {
                    _sceneUniformLayout
                },
                outputDescription: new OutputDescription(
                    new OutputAttachmentDescription(PixelFormat.D32_Float_S8_UInt),
                    new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm),
                    new OutputAttachmentDescription(PixelFormat.R32_SInt)),
                blendState: new BlendStateDescription(RgbaFloat.White, BlendAttachmentDescription.OverrideBlend, BlendAttachmentDescription.Disabled),
                rasterizerState: new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false)
                );

            _cam = _simpleCam;

            Random rng = new Random();

            //for (int i = 0; i < 10; i++)
            //{
            //    float s = 1 + i * 0.25f;

            //    Vector4 highlight = (rng.Next(2) == 0) ? new Vector4(1, 1f, 0.5f, 0.25f) : default;

            //    if (i == 5)
            //        highlight = new Vector4(-2f);

            //    var obj = new SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture>(Matrix4x4.CreateScale(s) * Matrix4x4.CreateTranslation(i * s * 2, 0, 0), i + 1, _cubeRenderer, highlight);

            //    _objects.Add(obj);
            //}

            //_plane = new SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture>(Matrix4x4.CreateTranslation(0, 0, 0), 0, _planeRenderer);
            //_ghostObj = new SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture>(Matrix4x4.CreateTranslation(0, 2, 2), 0, _cubeRenderer, new Vector4(-1f));

            //_bugfixObject = new SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture>(Matrix4x4.CreateScale(0, 0, 0), 0, _cubeRenderer, Vector4.One);

            //_objects.Add(_bugfixObject);

            


            CreateDeviceResources += (factory) =>
            {
                Debug.Assert(_sceneFramebuffer != null);

                //_cubeRenderer.CreateResources(factory, GD, _sceneFramebuffer.OutputDescription);
                //_planeRenderer.CreateResources(factory, GD, _sceneFramebuffer.OutputDescription);

                foreach (var obj in _objects)
                {
                    (obj as IDrawable)?.CreateGraphicsResources(factory);
                }

                //_ghostObj.CreateGraphicsResources(factory);
                //_plane.CreateGraphicsResources(factory);
            };
            #endregion
        }

        protected unsafe override void CreateResources(ResourceFactory factory)
        {
            Debug.Assert(GD != null);

            _gizmoDrawer = new GizmoDrawer(new Vector2(Width, Height), _cam);

            var deviceTex = orientationCubeTexture.CreateDeviceTexture(GD, factory);

            GizmoDrawer.SetOrientationCubeTexture(ImGuiRenderer.GetOrCreateImGuiBinding(factory,
                deviceTex));

            VeldridUtils.VertexFullscreenQuad[] verts = VeldridUtils.GetFullScreenQuadVerts(GD);

            

            _fullscreenQuadRenderer = new GenericObjectRenderer<ushort, VeldridUtils.VertexFullscreenQuad>(
                verts, VeldridUtils.QuadIndices, 
                Encoding.UTF8.GetBytes(FullscreenQuad_VertexCode), Encoding.UTF8.GetBytes(FullscreenQuad_FragmentCode),

                unformSetLayouts: new ShaderUniformLayout[]
                {
                    _sceneUniformLayout,
                    ShaderUniformLayoutBuilder.Get()
                        .AddTexture("Color0Texture",         ShaderStages.Fragment)
                        .AddTexture("Color1Texture",         ShaderStages.Fragment)
                        .AddTexture("Depth0Texture",         ShaderStages.Fragment)
                        .AddTexture("Depth1Texture",         ShaderStages.Fragment)
                        .AddSampler("TextureSampler",        ShaderStages.Fragment)
                        .AddTexture("Picking1Texture",       ShaderStages.Fragment)
                        .AddSampler("PickingSampler",        ShaderStages.Fragment)

                        .BeginUniformBuffer("ub_HoverColor", ShaderStages.Fragment)
                            .AddUniform<Vector4>("HoverColor")
                        .EndUniformBuffer()
                        .GetLayout()
                },
                outputDescription: new OutputDescription(
                    null,
                    new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm)),

                depthState: DepthStencilStateDescription.Disabled,
                blendState: new BlendStateDescription(RgbaFloat.White, new BlendAttachmentDescription(
                    true, BlendFactor.SourceAlpha, BlendFactor.InverseSourceAlpha, BlendFunction.Add, BlendFactor.One, BlendFactor.One, BlendFunction.Maximum)
                    )
                );

            var finalOutputDescription = new OutputDescription(null, new[] { new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm) });

            _fullscreenQuadRenderer.CreateResources();

            _scenePickingStagingTexture = factory.CreateTexture(TextureDescription.Texture2D(2, 1, 1, 1, PixelFormat.R32_SInt, TextureUsage.Staging));

            _sceneDepthCopyTexture = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _sceneDepthStagingTexture = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_Float, TextureUsage.Staging));


            _depthCopyFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(null, _sceneDepthCopyTexture));


            _depthCopyQuadRenderer = new GenericObjectRenderer<ushort, VeldridUtils.VertexFullscreenQuad>(verts, VeldridUtils.QuadIndices, Encoding.UTF8.GetBytes(VertexCodeDepthCopyQuad), Encoding.UTF8.GetBytes(FragmentCodeDepthCopyQuad),
                unformSetLayouts: new ShaderUniformLayout[]{
                    _sceneUniformLayout,
                    ShaderUniformLayoutBuilder.Get()
                        .AddTexture("DepthTexture",      ShaderStages.Fragment)
                        .AddSampler("TextureSampler",    ShaderStages.Fragment)
                        .GetLayout()
                    },
                depthState: DepthStencilStateDescription.Disabled, outputDescription: _depthCopyFramebuffer.OutputDescription) ;

            _depthCopyQuadRenderer.CreateResources();





            _ubScene = VeldridUtils.CreateUniformBuffer(new SceneUB());
            _ubScene.Name = "ub_Scene";

            _ubHoverColer = VeldridUtils.CreateUniformBuffer(new Vector4());
            _ubHoverColer.Name = "ub_HoverColor";

            _sceneResourceSet = _sceneUniformLayout.CreateResourceSet(_ubScene);



            CreateWindowSizeBoundResources(factory);


            

            

            CreateDeviceResources?.Invoke(factory);
        }

        public void CreateWindowSizeBoundResources(ResourceFactory factory)
        {
            Debug.Assert(GD != null);

            _sceneColor0Texture?.Dispose();
            _sceneColor0Texture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));

            _sceneColor1Texture?.Dispose();
            _sceneColor1Texture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));


            _sceneDepth0Texture?.Dispose();
            _sceneDepth0Texture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.D32_Float_S8_UInt, TextureUsage.DepthStencil | TextureUsage.Sampled));

            _sceneDepth1Texture?.Dispose();
            _sceneDepth1Texture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.D32_Float_S8_UInt, TextureUsage.DepthStencil | TextureUsage.Sampled));



            _scenePickingCTexture?.Dispose();
            _scenePickingCTexture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.R32_SInt, TextureUsage.RenderTarget | TextureUsage.Sampled));


            _finalTexture?.Dispose();
            _finalTexture = factory.CreateTexture(TextureDescription.Texture2D(MainSwapchain.Framebuffer.Width, MainSwapchain.Framebuffer.Height,
                1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled | TextureUsage.Storage));

            Debug.Assert(ImGuiRenderer!= null);

            _finalTextureBinding = ImGuiRenderer.GetOrCreateImGuiBinding(factory, _finalTexture);

            _sceneFramebuffer               = factory.CreateFramebuffer(new FramebufferDescription(_sceneDepth0Texture, _sceneColor0Texture, _scenePickingCTexture));
            _sceneHighlightFramebuffer      = factory.CreateFramebuffer(new FramebufferDescription(_sceneDepth1Texture, _sceneColor1Texture, _scenePickingCTexture));
            _finalFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(null, _finalTexture));

            _sceneColor0Texture.Name = "Color0Texture";
            _sceneColor1Texture.Name = "Color1Texture";

            _sceneDepth0Texture.Name = "Depth0Texture";
            _sceneDepth1Texture.Name = "Depth1Texture";

            _scenePickingCTexture.Name = "PickingCommonTexture";

            _finalTexture.Name = "FinalTexture";


            Debug.Assert(_fullscreenQuadRenderer != null);

            if (_fullscreenQuadParamsSets!=null)
                foreach (var set in _fullscreenQuadParamsSets) set?.Dispose();

            _fullscreenQuadParamsSets = new ResourceSet[]
            {
                _sceneResourceSet!,
                _fullscreenQuadRenderer.CreateResourceSet(1,

                    ("Color0Texture",       _sceneColor0Texture),
                    ("Color1Texture",       _sceneColor1Texture),
                    ("Depth0Texture",       _sceneDepth0Texture),
                    ("Depth1Texture",       _sceneDepth1Texture),
                    ("Picking1Texture",     _scenePickingCTexture),
                    ("TextureSampler",      GD.LinearSampler),
                    ("PickingSampler",      GD.PointSampler),
                    ("ub_HoverColor",       _ubHoverColer)

                )
            };

            Debug.Assert(_depthCopyQuadRenderer != null);

            if (_depthCopyQuadParamsSets != null)
                foreach (var set in _depthCopyQuadParamsSets) set?.Dispose();

            _depthCopyQuadParamsSets = new ResourceSet[]
            {
                _sceneResourceSet!,
                _depthCopyQuadRenderer.CreateResourceSet(1, 
                factory.CreateTextureView(_sceneDepth0Texture),
                    GD.PointSampler)
            };
        }

        private Dictionary<ResourceSet, BindableResource[]> _resourcesPerResourceSet = new();
        private Vector3 _positonUnderMouse;
        private bool _canAddObject;
        private Matrix4x4 _lastViewMatrix;
        private ImageSharpTexture orientationCubeTexture;

        private ResourceSet HandleResourceSet((ResourceSet resourceSet, BindableResource[] shaderResources) p)
        {
            _resourcesPerResourceSet[p.resourceSet] = p.shaderResources;

            return p.resourceSet;
        }

        protected override void OnDeviceDestroyed()
        {
            base.OnDeviceDestroyed();
        }

        protected override void HandleWindowResize()
        {
            base.HandleWindowResize();

            CreateWindowSizeBoundResources(ResourceFactory);

            

            _gizmoDrawer?.UpdateScreenSize(new Vector2(Width, Height));
        }

        protected override void Draw(float deltaSeconds, CommandList cl)
        {
            if (cl == null)
                throw new NullReferenceException($"The {nameof(CommandList)} has not been created yet");

            if (_sceneResourceSet == null)
                throw new NullReferenceException($"The Scene-Params {nameof(ResourceSet)} has not been created yet");

            if (_gizmoDrawer == null)
                throw new NullReferenceException($"The {nameof(GizmoDrawer)} has not been created yet");

            _ticks += deltaSeconds * 1000f;

            _foregroundDrawlist = ImGui.GetForegroundDrawList();
            _drawlist = ImGui.GetBackgroundDrawList();

            ImGui.SetNextWindowPos(new Vector2(20, 10));
            ImGui.SetNextWindowSize(new Vector2(200, 100));

            ImGui.Begin("", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove);

            ImGui.PushFont(MainFont ?? null);

            //lastFps = (1 / Math.Max(0.01f,deltaSeconds))*0.5f + lastFps * 0.5f;
            _lastFps = 1 / Math.Max(0.01f, deltaSeconds);

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 1f, 0.5f, 1));
            ImGui.Text($"fps = {_lastFps:#}");
            ImGui.Text($"depth = {_currentMouseDepth:0.#}");
            ImGui.Text($"pickingIndex = {_currentHoveredID}");
            //ImGui.Button("Test");
            ImGui.PopStyleColor();

            //ImGui.ShowDemoWindow();

            ImGui.End();

            var mousePos = InputTracker.MousePosition;

            

            Vector2 halfSize = new Vector2(Width, Height) * 0.5f;

            float aspectRatio = (float)Width / Height;

            Vector2 normCoords = (mousePos - halfSize) / halfSize;

            //using the calculation from whitehole
            float factorY = (float)Math.Tan(_fov * .5f);
            float factorX = factorY * aspectRatio;




            if (!_hasActiveAction && InputTracker.GetMouseButtonDown(MouseButton.Left))
            {
                _canAddObject = true;

                //_ghostObj.UpdateHighlight(new Vector4(0f));
            }

            if (!_hasActiveAction && InputTracker.GetMouseButtonUp(MouseButton.Left))
            {
                //_ghostObj.UpdateHighlight(new Vector4(-1f));

                //if (_canAddObject)
                //{
                //    var obj = new SimpleTestObject<ushort, VeldridUtils.VertexPositionTexture>(Matrix4x4.CreateTranslation(_positonUnderMouse), 0, _cubeRenderer);

                //    obj.CreateGraphicsResources(ResourceFactory);

                //    _objects.Add(obj);
                //}
            }

            if (ImGui.IsMouseDragging(0))
                _canAddObject = false;


            const float FLY_SPEED = 50;

            if (InputTracker.GetKey(Key.D))
                _camTarget += _cam.RightVector * deltaSeconds * FLY_SPEED;
            if (InputTracker.GetKey(Key.A))
                _camTarget -= _cam.RightVector * deltaSeconds * FLY_SPEED;
            if (InputTracker.GetKey(Key.W))
                _camTarget += _currentCameraRay * deltaSeconds * FLY_SPEED;
            if (InputTracker.GetKey(Key.S))
                _camTarget -= _currentCameraRay * deltaSeconds * FLY_SPEED;
            if (InputTracker.GetKey(Key.E))
                _camTarget.Y += deltaSeconds * FLY_SPEED;
            if (InputTracker.GetKey(Key.Q))
                _camTarget.Y -= deltaSeconds * FLY_SPEED;


            float depth = _currentMouseDepth;
            float scrollDelta = (InputTracker.MouseWheelDelta * 100 * Math.Min(InputTracker.GetKey(Key.ShiftLeft) ? 0.04f : 0.01f, depth / 500f));

            _camTarget += _currentCameraRay * scrollDelta;

            _dragMouseDepth -= Vector3.Dot(_cam.Position - _lastCameraPosition, _cam.ForwardVector);

            _lastCameraPosition = _cam.Position;

            if (!_hasActiveAction && InputTracker.GetMouseButtonDown(MouseButton.Left))
            {
                if(_currentMouseDepth == _far)
                    _dragMouseDepth = Math.Max(1f, _dragMouseDepth);
                else
                    _dragMouseDepth = _currentMouseDepth;
            }

            _positonUnderMouse = _cam.Position + _currentCameraRay * _currentMouseDepth;

            if (!_hasActiveAction && InputTracker.GetMouseButtonDown(MouseButton.Right) && InputTracker.ModifierKeys == ModifierKeys.Control && _currentMouseDepth<_far)
            {
                _camTarget = _positonUnderMouse;
            }

            var delta = mousePos - _lastMousePos;

            var deltaNorm = delta / halfSize;

            if (!_hasActiveAction && ImGui.IsMouseDragging(ImGuiMouseButton.Left) && InputTracker.GetMouseButton(MouseButton.Left))
            {
                float moveDepth = Math.Max(0.01f, _dragMouseDepth);
                _camTarget -= _cam.RightVector * deltaNorm.X * Math.Min(50f, moveDepth * factorX);
                _camTarget += _cam.UpVector    * deltaNorm.Y * Math.Min(50f, moveDepth * factorY);
            }



            if (!_hasActiveAction && ImGui.IsMouseDragging(ImGuiMouseButton.Middle) && InputTracker.GetMouseButton(MouseButton.Right))
            {
                _camYaw -= delta.X * 0.003f;
                _camPitch += delta.Y * 0.003f;

                _foregroundDrawlist.AddCircle(halfSize, 15, 0x88_FF_FF_FF, 32, 1.5f);
            }

            _lastMousePos = mousePos;



            


            bool hovered = mousePos.X < 100 && mousePos.Y < 100;

            if (hovered)
                _foregroundDrawlist.AddLine(new Vector2(630, 330), mousePos, 0xFF_FF_FF_FF, 1.5f);

            _lastHoveredID = _currentHoveredID;


            ProjectionMatrix = VeldridUtils.CreatePerspective(GD, false,
                _fov,
                aspectRatio,
                _near,
                _far);




            bool blending = InputTracker.GetKey(Key.B);

            var rotMtx = Matrix4x4.CreateRotationY(-_camYaw) * Matrix4x4.CreateRotationX(_camPitch);

            ViewMatrix = Matrix4x4.CreateTranslation(-_camTarget) * rotMtx * Matrix4x4.CreateTranslation(0, 0, -10);


            blending &= ViewMatrix == _lastViewMatrix;

            //if (ImGui.GetFrameCount() % 4 == 0)
            //    blending = false;

            float alphaBlend = 1;

            if (blending)
                alphaBlend = 0.1f;

            

            var viewProjectionMat = ViewMatrix * ProjectionMatrix;

            _simpleCam.SetViewMatrix(ViewMatrix);
            _simpleCam.SetProjectionMatrix(ProjectionMatrix);

            _gizmoDrawer.BeginFrame(_drawlist);

            if (blending)
            {
                viewProjectionMat *= Matrix4x4.CreateTranslation(Vector3.UnitX * (float)(Math.Sin(_ticks * 0.2) * 0.5 / Width));
                viewProjectionMat *= Matrix4x4.CreateTranslation(Vector3.UnitY * (float)(Math.Sin(_ticks * 0.4) * 0.5 / Height));
            }

            

            //viewProjectionMat = Matrix4x4.Transpose(viewProjectionMat);

            _lastViewMatrix = ViewMatrix;

            Vector3 camPlaneNormal = -_cam.ForwardVector / _far;


            var copy = ViewMatrix;

            

            copy.Translation = Vector3.Zero;

            _ubSceneData.View = viewProjectionMat;
            _ubSceneData.CamPlaneNormal = camPlaneNormal;
            _ubSceneData.CamPlaneOffset = Vector3.Dot(camPlaneNormal, _cam.Position);



            Debug.Assert(_fullscreenQuadParamsSets != null);
            Debug.Assert(_fullscreenQuadRenderer != null);

            if (InputTracker.GetKey(Key.ControlLeft))
            {

                if (InputTracker.GetKey(Key.ShiftLeft))
                    GD.UpdateBuffer(_ubHoverColer, 0, new Vector4(1, 0.0f, 0.0f, 1.2f));
                else
                    GD.UpdateBuffer(_ubHoverColer, 0, new Vector4(1, 0.5f, 0.2f, 1.0f));
            }
            else
                GD.UpdateBuffer(_ubHoverColer, 0, new Vector4(1, 1, 1, 0.8f));

            
            if(GD.IsUvOriginTopLeft)
                _drawlist.AddImage(_finalTextureBinding, new Vector2(0, 0), new Vector2(Width, Height));
            else
                _drawlist.AddImage(_finalTextureBinding, new Vector2(0, Height), new Vector2(Width, 0));

            var fb = MainSwapchain.Framebuffer;




            _gizmoDrawer.ClippedLine(Vector3.Zero, Vector3.UnitX * 100f, AxisInfo.Axis0.Color, 1.5f);
            _gizmoDrawer.ClippedLine(Vector3.Zero, Vector3.UnitY * 100f, AxisInfo.Axis1.Color, 1.5f);
            _gizmoDrawer.ClippedLine(Vector3.Zero, Vector3.UnitZ * 100f, AxisInfo.Axis2.Color, 1.5f);


            if (InputTracker.GetKeyDown(Key.Z))
                _camTarget = Vector3.Zero;



            if (InputTracker.GetKeyDown(Key.O) && InputTracker.ModifierKeys == ModifierKeys.Control)
            {
                WindowCreateInfo windowCI = new()
                {
                    X = 400,
                    Y = 100,
                    WindowWidth = 960,
                    WindowHeight = 540,
                    WindowTitle = "EditTK Playground 2"
                };

                var w = new PlaygroundWindow(windowCI);

                w.Run();
            }


            //(Vector3 axis, double angle) = MathUtils3D.GetSlerpAxisAngle(Matrix4x4.Identity, _gizmo.ObjectMatrix);

            ////_gizmoDrawer.DrawClippedLine(Vector3.Zero, axis * 3f, 0xFF_FF_FF_FF, 1.5f);

            ////Vector2 center2d = _gizmoDrawer.WorldToScreen(axis * 5f);

            ////_drawlist.AddText(
            ////    ImGui.GetFont(), 18,
            ////    center2d + new Vector2(RotationGizmo.GIMBAL_SIZE * 2, -RotationGizmo.GIMBAL_SIZE * 0.5f), AxisInfo.ViewRotationAxis.Color,
            ////    $"angle : {angle * MathUtils.RADIANS_TO_DEGREES :0.#}°");

            //if (InputTracker.GetKeyDown(Key.R))
            //    _gizmo = new RotationGizmo(_gizmo.ObjectMatrix * Matrix4x4.CreateFromAxisAngle(axis, -(float)angle), false);


            _currentCameraRay =
                _cam.RightVector * normCoords.X * factorX +
                _cam.UpVector * -normCoords.Y * factorY +
                _cam.ForwardVector;

            //ActionUpdateResult res = _gizmo.Update(_gizmoDrawer, _cam, in _currentCameraRay, out bool gizmoIsHovered, out _hasActiveAction);

            _gizmoDrawer.TranslationGizmo(Matrix4x4.Identity, 64, out _);
            //_gizmoDrawer.RotationGizmo(Matrix4x4.Identity, 64, out _);

            _gizmoDrawer.OrientationCube(new Vector2(Width - 100, Height - 100), 50, out _);




            bool mouseIsInBounds = (mousePos.X >= 0 && mousePos.X < Width) && (mousePos.Y >= 0 && mousePos.Y < Height);

            cl.SetFramebuffer(_sceneFramebuffer);
            cl.ClearColorTarget(0, new RgbaFloat(0,0,0.2f,1));
            cl.ClearColorTarget(1, RgbaFloat.Clear);
            cl.ClearDepthStencil(1f);

            _ubSceneData.ViewportSize = new Vector2(Width, Height);
            _ubSceneData.ForceSolidHighlight = 0;
            _ubSceneData.BlendAlpha = 0;

            cl.UpdateBuffer(_ubScene, 0, _ubSceneData);


            //(_objects[0] as ITransformable)?.UpdateTransform(_gizmo.ObjectMatrix);


            foreach (var obj in _objects)
            {
                (obj as IDrawable)?.Draw(cl, _sceneResourceSet, Pass.OPAQUE);
            }

            _planeRenderer.Draw(cl, _sceneResourceSet);


            if (mouseIsInBounds)
            {
                Debug.Assert(_depthCopyQuadRenderer != null);

                _depthCopyQuadRenderer._model.EnsureResourcesCreated();

                cl.SetFramebuffer(_depthCopyFramebuffer);

                Vector2 depthCopyUV = mousePos / new Vector2(fb.Width, fb.Height);

                if (!GD.IsUvOriginTopLeft)
                    depthCopyUV.Y = 1 - depthCopyUV.Y;

                var verts = VeldridUtils.GetFullScreenCopyQuadVerts(GD, depthCopyUV);
                GD.UpdateBuffer(_depthCopyQuadRenderer.VertexBuffer, 0, verts);

                _depthCopyQuadRenderer.Draw(cl, _depthCopyQuadParamsSets!);


                Debug.Assert(_scenePickingCTexture != null);

                float x = mousePos.X;
                float y = mousePos.Y;

                if (!GD.IsUvOriginTopLeft)
                    y = fb.Height - y - 1;

                if (x >= _scenePickingCTexture.Width)
                    x = _scenePickingCTexture.Width - 1;

                if (y >= _scenePickingCTexture.Height)
                    y = _scenePickingCTexture.Height - 1;


                cl.CopyTexture(
                    _sceneDepthCopyTexture, 0, 0, 0, 0, 0,
                    _sceneDepthStagingTexture, 0, 0, 0, 0, 0,
                    1, 1, 1, 1);

                cl.CopyTexture(
                    _scenePickingCTexture, (uint)x, (uint)y, 0, 0, 0,
                    _scenePickingStagingTexture, 1, 0, 0, 0, 0,
                    1, 1, 1, 1);
            }

            cl.SetFramebuffer(_sceneFramebuffer);

            //_ghostObj.SetTransform(Matrix4x4.CreateTranslation(_positonUnderMouse));

            //_ghostObj.Draw(cl, _sceneResourceSet, Pass.OPAQUE);


            _ubSceneData.ForceSolidHighlight = 1;

            cl.UpdateBuffer(_ubScene, 0, _ubSceneData);

            cl.SetFramebuffer(_sceneHighlightFramebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);
            cl.ClearColorTarget(1, RgbaFloat.Clear);
            cl.ClearDepthStencil(1f, 0);

            foreach (var obj in _objects)
            {
                (obj as IDrawable)?.Draw(cl, _sceneResourceSet, Pass.HIGHLIGHT_ONLY);
            }

            if (mouseIsInBounds)
            {
                float x = mousePos.X;
                float y = mousePos.Y;

                if (!GD.IsUvOriginTopLeft)
                    y = fb.Height - y - 1;

                cl.CopyTexture(
                    _scenePickingCTexture, (uint)x, (uint)y, 0, 0, 0,
                    _scenePickingStagingTexture, 0, 0, 0, 0, 0,
                    1, 1, 1, 1);
            }


            _ubSceneData.CamPlaneNormal = Vector3.Zero;

            cl.UpdateBuffer(_ubScene, 0, _ubSceneData);

            if (_currentHoveredID>0)
            {
                (_objects[_currentHoveredID - 1] as IDrawable)?.Draw(cl, _sceneResourceSet, Pass.OPAQUE);
            }



            //make alpha blend frame independant
            alphaBlend = (float)(1 - Math.Pow(1 - alphaBlend, deltaSeconds * 60));

            _ubSceneData.BlendAlpha = alphaBlend;

            cl.UpdateBuffer(_ubScene, 0, _ubSceneData);

            cl.SetFramebuffer(_finalFramebuffer);
            //cl.ClearColorTarget(0, RgbaFloat.Black);

            _fullscreenQuadRenderer?.Draw(cl, _fullscreenQuadParamsSets);


            cl.SetFramebuffer(MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);

            if (mouseIsInBounds)
            {


                MappedResourceView<float> mappedDepth = GD.Map<float>(_sceneDepthStagingTexture, MapMode.Read);



                _currentMouseDepth = mappedDepth[0] * _far;

                GD.Unmap(_sceneDepthStagingTexture);

                if (!_gizmoDrawer.IsAnythingHovered())
                {
                    MappedResourceView<int> mappedPicking = GD.Map<int>(_scenePickingStagingTexture, MapMode.Read);

                    _currentHoveredID = mappedPicking[0];

                    var xrayID = mappedPicking[1];

                    if (xrayID != 0)
                        _currentHoveredID = xrayID;

                    GD.Unmap(_scenePickingStagingTexture);
                }
                else
                    _currentHoveredID = 0;
            }
            else
                _currentHoveredID = 0;

            //if (gizmoIsHovered)
            //    _currentHoveredID = 0;

            

            if(_lastCleanupTicks + 10000 < _ticks)
            {
                GC.Collect(0);

                _lastCleanupTicks = _ticks;
            }
        }


        private static VeldridUtils.VertexPositionTexture[] GetCubeVertices()
        {
            var vertices = new VeldridUtils.VertexPositionTexture[]
            {
                // Top
                new (new Vector3(-1f, +1f, -1f), new Vector2(0, 0)),
                new (new Vector3(+1f, +1f, -1f), new Vector2(1, 0)),
                new (new Vector3(+1f, +1f, +1f), new Vector2(1, 1)),
                new (new Vector3(-1f, +1f, +1f), new Vector2(0, 1)),
                // Bottom                                                             
                new (new Vector3(-1f,-1f, +1f),  new Vector2(0, 0)),
                new (new Vector3(+1f,-1f, +1f),  new Vector2(1, 0)),
                new (new Vector3(+1f,-1f, -1f),  new Vector2(1, 1)),
                new (new Vector3(-1f,-1f, -1f),  new Vector2(0, 1)),
                // Left                                                               
                new (new Vector3(-1f, +1f, -1f), new Vector2(0, 0)),
                new (new Vector3(-1f, +1f, +1f), new Vector2(1, 0)),
                new (new Vector3(-1f, -1f, +1f), new Vector2(1, 1)),
                new (new Vector3(-1f, -1f, -1f), new Vector2(0, 1)),
                // Right                                                              
                new (new Vector3(+1f, +1f, +1f), new Vector2(0, 0)),
                new (new Vector3(+1f, +1f, -1f), new Vector2(1, 0)),
                new (new Vector3(+1f, -1f, -1f), new Vector2(1, 1)),
                new (new Vector3(+1f, -1f, +1f), new Vector2(0, 1)),
                // Back                                                               
                new (new Vector3(+1f, +1f, -1f), new Vector2(0, 0)),
                new (new Vector3(-1f, +1f, -1f), new Vector2(1, 0)),
                new (new Vector3(-1f, -1f, -1f), new Vector2(1, 1)),
                new (new Vector3(+1f, -1f, -1f), new Vector2(0, 1)),
                // Front                                                              
                new (new Vector3(-1f, +1f, +1f), new Vector2(0, 0)),
                new (new Vector3(+1f, +1f, +1f), new Vector2(1, 0)),
                new (new Vector3(+1f, -1f, +1f), new Vector2(1, 1)),
                new (new Vector3(-1f, -1f, +1f), new Vector2(0, 1)),
            };

            return vertices;
        }

        private static ushort[] GetCubeIndices()
        {
            ushort[] indices =
            {
                0,1,2, 0,2,3,
                4,5,6, 4,6,7,
                8,9,10, 8,10,11,
                12,13,14, 12,14,15,
                16,17,18, 16,18,19,
                20,21,22, 20,22,23,
            };

            return indices;
        }

        private const string Cube_VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 1, binding = 0) uniform ub_World
{
    mat4 World;
};

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec3 fragPos;
layout(location = 1) out vec3 fragNrm;
layout(location = 2) out vec4 fragColor;
layout(location = 3) flat out vec3 boxScale;
layout(location = 4) out float fragDepth;

void main() {
    fragNrm = vec3(0,1,0);
    fragColor = vec4(0);
   
    boxScale = vec3(
        length(World[0].xyz),
        length(World[1].xyz),
        length(World[2].xyz));
   
    mat4 _World = mat4(
        World[0]/boxScale.x*0.5,
        World[1]/boxScale.y*0.5,
        World[2]/boxScale.z*0.5,
        World[3]
    );
   
    vec3 _pos = round(pos);
    vec3 _poss = _pos * boxScale;
    _poss += pos-_pos;
   
    fragPos = _poss;

    vec4 worldPos = _World * vec4(_poss, 1);
   
    gl_Position = View * worldPos;

    fragDepth = (CamPlaneOffset-dot(worldPos.xyz,CamPlaneNormal));
}
";

        private const string Cube_FragmentCode = @"
#version 450

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 1, binding = 1) uniform ub_PickingID_Highlight
{
    int PickingID;
    vec4 Highlight;
};

#define min3(a,b,c) min(a,min(b,c))

float eval(float signedDistance, float w){
    return smoothstep(0.0,w,-signedDistance);
}

float evalShd(float signedDistance, float r){
    return smoothstep(-r,0.0,-signedDistance)*0.2;
}

float sdCapsule( vec2 p, vec2 a, vec2 b, float r )
{
  vec2 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return length( pa - ba*h ) - r;
}

vec3 color = vec3(0,0.25,1);

layout(location = 0) in vec3 fragPos;
layout(location = 1) in vec3 fragNrm;
layout(location = 2) in vec4 fragColor;
layout(location = 3) flat in vec3 boxScale;
layout(location = 4) in float fragDepth;

layout(location = 0) out vec4 outColor;
layout(location = 1) out int outPickingID;

void main() {
    
    float w = fwidth(fragPos.x);
    float r = 0.125 * min(boxScale.x,boxScale.z);
    
    
    
    vec3 dist = vec3(1)-(boxScale - abs(fragPos));
    float a = min3(max(dist.x, dist.y),max(dist.x, dist.z),max(dist.y, dist.z));
    a = max(0,1-(1-a)*4);
    //float a = fragColor.r;
    //float a = abs(fragNrm.x)+abs(fragNrm.y)+abs(fragNrm.z);
    //a = smoothstep(1.1,1.6,a);
    a *= a*a*a*a;
    
    vec2 pos2d = fragPos.xz;
    
    vec2 s = boxScale.xz;
    
    float d = sdCapsule(pos2d, vec2(-.5,0)*s,vec2(0,.5)*s, r);
    
    d = min(d,sdCapsule(pos2d, vec2(.5,0)*s,vec2(0,.5)*s, r));
    
    float l = smoothstep(-1,1,fragNrm.y);
    
    outColor = vec4(color*
    mix(0.2*l+0.1,1.0, eval(d,w)+evalShd(d,r))*((fragPos.y/boxScale.y)*0.25+0.75),
    1);
    outColor = mix(outColor, vec4(color,1), a);
    
    float h_a = mix(Highlight.a, 1, ForceSolidHighlight);
    
    outColor.rgb = mix(outColor.rgb, Highlight.rgb, h_a * float(Highlight.w>=0.0));
    
    float _discard = float(Highlight.w==-1.0 && mod(gl_FragCoord.x,2.0) != mod(gl_FragCoord.y,2.0));

    vec2 fragMod = mod(floor(gl_FragCoord.xy/10.0),2.0);

    outColor.rgb = mix(outColor.rgb, vec3(1.0,0.2,0.2), min(0.5+float(fragMod.x != fragMod.y),1.0) * float(Highlight.w==-2.0)*0.5);
    
    gl_FragDepth =  fragDepth + _discard * 2.0;
    
    outPickingID = PickingID;
}";


        private const string Plane_VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 1, binding = 0) uniform ub_World
{
    mat4 World;
};

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec3 fragPos;
layout(location = 1) out float fragDepth;

void main() {

    vec4 worldPos = World * vec4(pos, 1);

    fragPos = worldPos.xyz;
   
    gl_Position = View * vec4(fragPos, 1);

    fragDepth = (CamPlaneOffset-dot(worldPos.xyz,CamPlaneNormal));
}
";

        private const string Plane_FragmentCode = @"
#version 450

#define max3(a,b,c) max(max(a,b),c);

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 1, binding = 1) uniform ub_PickingID_Highlight
{
    int PickingID;
    vec4 Highlight;
};

layout(location = 0) in vec3 fragPos;
layout(location = 1) in float fragDepth;

layout(location = 0) out vec4 outColor;
layout(location = 1) out int outPickingID;

float oneOverLogOfTen = 1.0/log(10.0);

//inspired by blender checker texture node
float checker(vec3 p, float checkerSize)
{
	int xi = int(floor(p.x/checkerSize));
	int yi = int(floor(p.y/checkerSize));
	int zi = int(floor(p.z/checkerSize));

    vec3 _p = p / checkerSize - 0.5;

    vec3 w = fwidth(_p);
    vec3 i = clamp((abs(mod(_p,2.0)-1.0)-0.5)/w,-0.5,0.5)+0.5;
    return abs(abs(i.x-i.z)-i.y);
}

float blendChecker(vec3 p){
	
	vec3 gradientVec = vec3(dFdy(p.x),dFdy(p.x),dFdy(p.z));

    float max_w = max3(fwidth(p.x),fwidth(p.x),fwidth(p.z));
	
	float l_w = max(1,log(max_w*100) * oneOverLogOfTen);

	float b = abs(mod(l_w, 2.0)-1.0);
    
    float checkerA = checker(p, pow(10.0,
        max(0.0,floor((l_w)/2.0)*2.0)));
    float checkerB = checker(p, pow(10.0,
        max(0.0,floor((l_w-1.0)/2.0)*2.0+1.0)));

	return 0.5+mix(checkerA,
	               checkerB,b)*0.5;
}



void main()
{
    //outColor = vec4(vec3(0.5)*blendChecker(fragPos), 1);
    outColor = vec4(1.0);
    outPickingID = PickingID;

    gl_FragDepth =  fragDepth;
}";




        private readonly string FullscreenQuad_VertexCode = 
            File.ReadAllText(SystemUtils.RelativeFilePath("shaders", "fullscreenQuad.vert"));

        private readonly string FullscreenQuad_FragmentCode = 
            File.ReadAllText(SystemUtils.RelativeFilePath("shaders","fullscreenQuad.frag"));
        
        private const string VertexCodeDepthCopyQuad = @"
#version 450

layout(location = 0) in vec2 pos;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec2 fragUV;

void main() {
    fragUV = uv;
   
    gl_Position = vec4(pos, 0.5, 1.0);
}
";

        private const string FragmentCodeDepthCopyQuad = @"
#version 450

layout(location = 0) in vec2 fragUV;

layout(location = 0) out vec4 outDepth;

layout(set = 2, binding = 0) uniform texture2D DepthTexture;
layout(set = 2, binding = 1) uniform sampler SurfaceSampler;

void main() {
   outDepth = texture(sampler2D(DepthTexture, SurfaceSampler), fragUV);
}";

        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
    }

    public struct PickingID_Highlight
    {
        public int PickingID;
        public int Padding1;
        public int Padding2;
        public int Padding3;
        public Vector4 Highlight;

        public PickingID_Highlight(int pickingID, Vector4 highlight)
        {
            PickingID = pickingID;
            Highlight = highlight;

            Padding1 = 0;
            Padding2 = 0;
            Padding3 = 0;
        }
    }

    //public class SimpleTestObject<TIndex, TVertex> : 
    //    IDrawable, ITransformable
    //    where TIndex : unmanaged 
    //    where TVertex : unmanaged
    //{
    //    GenericObjectRenderer<TIndex, TVertex> _modelRenderer;

    //    ResourceSet? _objectParams;
    //    DeviceBuffer? _ubTransform;
    //    DeviceBuffer? _ubPickingID_Highlight;

    //    Matrix4x4 _transform;
    //    Vector4 _highlight;

    //    readonly int _pickingID;

    //    public SimpleTestObject(Matrix4x4 transform, int pickingID, GenericObjectRenderer<TIndex, TVertex> modelRenderer, Vector4 highlight = new Vector4())
    //    {
    //        _transform = transform;
    //        _highlight = highlight;
    //        _pickingID = pickingID;
    //        _modelRenderer = modelRenderer;
    //    }

    //    public void CreateGraphicsResources(ResourceFactory factory)
    //    {
    //        _ubTransform = factory.CreateBuffer(new BufferDescription(VeldridUtils.SIZE_OF_MAT4, BufferUsage.UniformBuffer));
    //        _ubTransform.Name = "ub_Transform_"+_pickingID;

    //        _ubPickingID_Highlight = factory.CreateBuffer(new BufferDescription(VeldridUtils.GetMinimumBufferSize(sizeof(uint) + VeldridUtils.SIZE_OF_VEC4), BufferUsage.UniformBuffer));
    //        _ubPickingID_Highlight.Name = "ub_PickingID_Highlight_" + _pickingID;

    //        _objectParams = factory.CreateResourceSet(new ResourceSetDescription(SharedResources.ObjectParamsLayout, _ubTransform, _ubPickingID_Highlight));

    //        UpdateHighlight(_highlight);
    //        SetTransform(_transform);
    //    }

    //    public void DestroyGraphicsResources()
    //    {
    //        _ubTransform?.Dispose();
    //        _ubPickingID_Highlight?.Dispose();
    //        _objectParams?.Dispose();
    //    }

    //    public Matrix4x4 GetTransform() => _transform;

    //    public void SetTransform(Matrix4x4 transform)
    //    {
    //        _transform = transform;

    //        GD.UpdateBuffer(_ubTransform, 0, _transform);
    //    }

    //    public void ApplyTransformation(Matrix4x4 transformation)
    //    {
    //        _transform *= transformation;

    //        GD.UpdateBuffer(_ubTransform, 0, Matrix4x4.Identity);
    //    }

    //    public void UpdateHighlight(Vector4 highlight)
    //    {
    //        _highlight = highlight;

    //        GD.UpdateBuffer(_ubPickingID_Highlight, 0, new PickingID_Highlight(_pickingID, highlight));
    //    }

    //    public void Draw(CommandList cl, ResourceSet sceneParamsSet, Pass pass)
    //    {
    //        if (pass == Pass.HIGHLIGHT_ONLY && _highlight.W <= 0)
    //            return;


    //        _modelRenderer.Draw(cl, sceneParamsSet, _objectParams);
    //    }

    //    public bool HasHighlight() => _highlight.W > 0;
    //}
}
