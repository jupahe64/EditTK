
// DRAFT


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Veldrid;
//using Veldrid.SPIRV;

//namespace EditTK.Graphics.Common
//{
//    public class SimpleModelRenderer<TIndex, TVertex>
//        where TIndex : unmanaged
//        where TVertex : unmanaged
//    {
//        private readonly TVertex[] _vertices;
//        private readonly TIndex[] _indices;
//        private readonly byte[] _vertexShaderBytes;
//        private readonly byte[] _fragmentShaderBytes;
//        private BlendStateDescription? _blendState;
//        private DepthStencilStateDescription? _depthState;
//        private RasterizerStateDescription? _rasterizerState;
//        private readonly PrimitiveTopology? _topology;

//        private DeviceBuffer? _vertexBuffer;
//        private DeviceBuffer? _indexBuffer;
//        private Pipeline? _pipeline;
//        private ResourceLayout[] _resourceLayouts;
//        private ShaderSetDescription _shaderSet;
//        private readonly VertexAttributeAtrribute[] _vertexAttributeAtrributes;

//        private readonly ResourceLayout[] _extraResourceLayouts;

//        public IReadOnlyList<ResourceLayout> ResourceLayouts => _extraResourceLayouts;

//        public DeviceBuffer? VertexBuffer => _vertexBuffer;

//        private readonly IndexFormat _indexFormat;

//        private static readonly Regex uniformRegex = new("uniform[ \n\r]+([^ \n\r]*)[ \n\r]+([^ =]*)[ \n\r]*(=[ \n\r]*([^;]*))?;", RegexOptions.Compiled);

//        private static readonly Regex vecTypeRegex = new("(.)?vec(\\d)", RegexOptions.Compiled);

//        private static readonly Regex matTypeRegex = new("mat(\\d)(x(\\d))?", RegexOptions.Compiled);

//        static SimpleModelRenderer()
//        {

//        }

//        public SimpleModelRenderer(TVertex[] vertices, TIndex[] indices, string vertexShader, string fragmentShader,
//            BlendStateDescription? blendState = null, DepthStencilStateDescription? depthState = null,
//            RasterizerStateDescription? rasterizerState = null, PrimitiveTopology? topology = null)
//        {
//            _vertices = vertices;
//            _indices = indices;
//            _vertexShaderBytes = Encoding.UTF8.GetBytes(vertexShader);
//            _fragmentShaderBytes = Encoding.UTF8.GetBytes(fragmentShader);
//            _blendState = blendState;
//            _depthState = depthState;
//            _rasterizerState = rasterizerState;
//            _topology = topology;


//            List<(Type type, string name, object value)> collectedUniforms = new();

//            uniformRegex.Replace(vertexShader, match =>
//            {
//                var typeStr = match.Groups[1].Value;

//                Match typeMatch;

//                if((typeMatch = vecTypeRegex.Match(typeStr)).Success)
//                {

//                }

//                collectedUniforms.Add((typeof(float), match.Groups[2].Value, match.Groups[4].Value));
//                return "";
//            }
//            );




//            Type vertexStructType = typeof(TVertex);

//            var fieldInfos = vertexStructType.GetFields();

//            _vertexAttributeAtrributes = new VertexAttributeAtrribute[fieldInfos.Length];

//            for (int i = 0; i < fieldInfos.Length; i++)
//            {
//                var attribute = fieldInfos[i].GetCustomAttribute<VertexAttributeAtrribute>();

//                if (attribute == null)
//                    throw new ArgumentException($"Field {fieldInfos[i].Name} of the struct {vertexStructType.Name} has no {nameof(VertexAttributeAtrribute)}");

//                _vertexAttributeAtrributes[i] = attribute;
//            }

//            var indexStructName = typeof(TIndex).Name;

//            _indexFormat = indexStructName switch
//            {
//                nameof(UInt16) or nameof(Int16) => IndexFormat.UInt16,
//                nameof(UInt32) or nameof(Int32) => IndexFormat.UInt32,
//                _ => throw new ArgumentException($"The type {indexStructName} is not valid for {nameof(TIndex)}"),
//            };
//        }

//        public void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice, OutputDescription framebufferOutputDescription)
//        {
//            _vertexBuffer = factory.CreateBuffer(new BufferDescription(VeldridUtils.GetSizeInBytes(_vertices), BufferUsage.VertexBuffer));
//            _vertexBuffer.Name = "VertexBuffer (generated)";
//            graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);

//            _indexBuffer = factory.CreateBuffer(new BufferDescription(VeldridUtils.GetSizeInBytes(_indices), BufferUsage.IndexBuffer));
//            _indexBuffer.Name = "IndexBuffer (generated)";
//            graphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);


//            var res = SpirvCompilation.CompileVertexFragment(
//                _vertexShaderBytes,
//                _fragmentShaderBytes,
//                CrossCompileTarget.HLSL
//                );

//            _shaderSet = new ShaderSetDescription(
//                new[]
//                {
//                    new VertexLayoutDescription(
//                        _vertexAttributeAtrributes.Select(x=> x.Offset!=null?
//                        new VertexElementDescription(x.AttributeName, VertexElementSemantic.TextureCoordinate, x.AttributeFormat, x.Offset.Value):
//                        new VertexElementDescription(x.AttributeName, VertexElementSemantic.TextureCoordinate, x.AttributeFormat)).ToArray())
//                },
//                factory.CreateFromSpirv(
//                    new ShaderDescription(ShaderStages.Vertex, _vertexShaderBytes, "main", true),
//                    new ShaderDescription(ShaderStages.Fragment, _fragmentShaderBytes, "main", true)));

//            _resourceLayouts = res.Reflection.ResourceLayouts.Select(x=>factory.CreateResourceLayout(x)).ToArray();

//            _resourceLayouts = new ResourceLayout[(_shaderResourceLayouts?.Length ?? 0) + COMMON_RESOURCE_SET_NUM];

//            _resourceLayouts[0] = SharedResources.SceneParamsLayout;
//            _resourceLayouts[1] = SharedResources.ObjectParamsLayout;

//            if (_shaderResourceLayouts != null)
//            {
//                for (int i = 0; i < _shaderResourceLayouts.Length; i++)
//                {
//                    ResourceLayout layout = _shaderResourceLayouts[i].CreateResourceLayout(factory, graphicsDevice);

//                    _resourceLayouts[i + COMMON_RESOURCE_SET_NUM] = layout;

//                    _extraResourceLayouts[i] = layout;
//                }
//            }

//            CreatePipeline(factory, framebufferOutputDescription);
//        }
//    }
//}
