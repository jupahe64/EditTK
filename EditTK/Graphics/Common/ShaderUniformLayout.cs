using EditTK.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace EditTK.Graphics.Common
{
    /// <summary>
    /// A builder class for creating a <see cref="ShaderUniformLayout"/>
    /// </summary>
    public sealed class ShaderUniformLayoutBuilder
    {
        public class BufferLayoutBuilder
        {
            private int _resourceIndex = -1;
            private uint _offset = 0;

            private readonly ShaderUniformLayoutBuilder _layoutBuilder;

            internal BufferLayoutBuilder(ShaderUniformLayoutBuilder layoutBuilder)
            {
                _layoutBuilder = layoutBuilder;
            }

            internal void Begin(int resourceIndex)
            {
                _resourceIndex = resourceIndex;
                _offset = 0;

            }

            /// <summary>
            /// Adds a uniform to the uniform buffer layout
            /// </summary>
            /// <typeparam name="T">The value type of the uniform</typeparam>
            public BufferLayoutBuilder AddUniform<T>(string uniformName) where T : unmanaged
            {
                UniformTypeCache.Register<T>();

                _layoutBuilder._uniforms.Add((uniformName, _resourceIndex, _offset));
                _offset += (uint)Unsafe.SizeOf<T>();
                return this;
            }

            public ShaderUniformLayoutBuilder EndUniformBuffer()
            {
                _layoutBuilder._uniformBufferSizes.Add((_resourceIndex, _offset));
                return _layoutBuilder;
            }
        }

        private readonly BufferLayoutBuilder _bufferLayoutBuilder;

        private readonly List<(string name, int resourceIndex, uint offset)> _uniforms = new();

        private readonly List<(int resourceIndex, uint size)> _uniformBufferSizes = new();

        private readonly List<ResourceLayoutElementDescription> _elements = new();



        private static readonly ShaderUniformLayoutBuilder _instance = new();

        private static bool _instanceReady = true;

        public static ShaderUniformLayoutBuilder Get()
        {
            if (!_instanceReady)
                throw new InvalidOperationException(
                    $"The static instance of {nameof(ShaderUniformLayoutBuilder)} is already in use. " +
                    $"Make sure to call {nameof(GetLayout)}() first");

            _instanceReady = false;
            return _instance;
        }

        /// <summary>
        /// Only use this constructor if you want to use more than one builder at a time
        /// <para>In every other case you should use the static instance by calling <see cref="Get"/></para>
        /// </summary>
        public ShaderUniformLayoutBuilder()
        {
            _bufferLayoutBuilder = new(this);
        }

        /// <summary>
        /// Adds a new resource uniform to the layout
        /// <para>!!! The order of resources should match the binding slots in the shader !!!</para>
        /// </summary>
        /// <param name="shaderStages">The shader stages (VertexShader, FragmentShader...) 
        /// this uniform will be used in</param>
        /// <param name="uniformName">Should match the name the uniform has in the shader</param>
        public ShaderUniformLayoutBuilder AddResourceUniform(string uniformName, ResourceKind kind, ShaderStages shaderStages)
        {
            _elements.Add(new ResourceLayoutElementDescription(uniformName, kind, shaderStages));
            return this;
        }

        /// <summary>
        /// Adds a new texture uniform to the layout
        /// <para>!!! The order of resources should match the binding slots in the shader !!!</para>
        /// </summary>
        /// <param name="shaderStages">The shader stages (VertexShader, FragmentShader...) 
        /// this uniform will be used in</param>
        /// <param name="uniformName">Should match the name the uniform has in the shader</param>
        public ShaderUniformLayoutBuilder AddTexture(string uniformName, ShaderStages shaderStages) =>
            AddResourceUniform(uniformName, ResourceKind.TextureReadOnly, shaderStages);

        /// <summary>
        /// Adds a new read-write texture uniform (called image1|2|3D in glsl) to the layout
        /// <para>!!! The order of resources should match the binding slots in the shader !!!</para>
        /// </summary>
        /// /// <param name="shaderStages">The shader stages (VertexShader, FragmentShader...) 
        /// this uniform will be used in</param>
        /// <param name="uniformName">Should match the name the uniform has in the shader</param>
        public ShaderUniformLayoutBuilder AddImage(string uniformName, ShaderStages shaderStages) =>
            AddResourceUniform(uniformName, ResourceKind.TextureReadWrite, shaderStages);

        /// <summary>
        /// Adds a new texture sampler uniform to the layout
        /// <para>!!!The order of uniforms should match the binding slots in the shader!!!</para>
        /// </summary>
        /// /// <param name="shaderStages">The shader stages (VertexShader, FragmentShader...) 
        /// this uniform will be used in</param>
        /// <param name="uniformName">Should match the name the uniform has in the shader</param>
        public ShaderUniformLayoutBuilder AddSampler(string uniformName, ShaderStages shaderStages) =>
            AddResourceUniform(uniformName, ResourceKind.Sampler, shaderStages);

        /// <summary>
        /// Adds a new uniform buffer to the layout and "opens" a <see cref="BufferLayoutBuilder"/>
        /// to further describe the buffers layout, that's optional though
        /// <para>!!!The order of uniforms should match the binding slots in the shader!!!</para>
        /// </summary>
        /// /// <param name="shaderStages">The shader stages (VertexShader, FragmentShader...) 
        /// this uniform will be used in</param>
        /// <param name="uniformName">Should match the name the uniform has in the shader</param>
        public BufferLayoutBuilder BeginUniformBuffer(string uniformName, ShaderStages shaderStages)
        {
            AddResourceUniform(uniformName, ResourceKind.UniformBuffer, shaderStages);
            _bufferLayoutBuilder.Begin(_elements.Count-1);
            return _bufferLayoutBuilder;
        }

        /// <summary>
        /// Concludes the construction and returns the constructed <see cref="ShaderUniformLayout"/>
        /// </summary>
        /// <returns></returns>
        public ShaderUniformLayout GetLayout()
        {
            ShaderUniformLayout layout = new(_elements, _uniforms, _uniformBufferSizes);

            if (this == _instance)
            {
                _elements.Clear();
                _uniforms.Clear();
                _uniformBufferSizes.Clear();

                _instanceReady = true;
            }

            return layout;
        }
    }



    /// <summary>
    /// A static cache that keeps track of all uniform value types that can be used in uniform buffers
    /// </summary>
    public static class UniformTypeCache
    {
        internal delegate void BufferWriter(byte[] buffer, uint offset, object value);

        private static readonly Dictionary<Type, BufferWriter> _writersByType = new();

        internal static BufferWriter GetBufferWriter(Type type)
        {
            if (!_writersByType.TryGetValue(type, out var bufferWriter))
                throw new ArgumentException($"Type {type.Name} is not registered, try calling" +
                    $"{nameof(UniformTypeCache)}.{nameof(UniformTypeCache.Register)}<{type.Name}>(); to register it");

            return bufferWriter;
        }

        static UniformTypeCache()
        {
            Register<int>();
            Register<uint>();
            Register<float>();
            Register<Vector2>();
            Register<Vector3>();
            Register<Vector4>();
            Register<Matrix3x2>();
            Register<Matrix4x4>();
        }

        /// <summary>
        /// Registers a new uniform value type
        /// </summary>
        /// <typeparam name="T">The uniform value type</typeparam>
        public static unsafe void Register<T>() where T : unmanaged
        {
            Type type = typeof(T);

            if (_writersByType.ContainsKey(type)) return;

            _writersByType[type] = (buffer, offset, value) =>
            {
                T data = Unsafe.Unbox<T>(value);

                Unsafe.Copy(Unsafe.AsPointer(ref buffer[offset]), ref data);
            };
        }
    }


    /// <summary>
    /// A class that represents the layout of uniforms inside a shaders uniform set (all uniforms that have the same set id)
    /// <para>This class has no constructor, use <see cref="ShaderUniformLayoutBuilder"/></para>
    /// </summary>
    public sealed class ShaderUniformLayout : ResourceHolder
    {
        private readonly Dictionary<string, int> _resourceIndexByParamName;
        private readonly Dictionary<string, (int index, uint offset)> _uniformValueIndexOffsetByParamName;
        private readonly Dictionary<int, uint> _uniformBufferSizes;

        /// <summary>
        /// The actual ResourceLayout
        /// </summary>
        public ResourceLayout? ResourceLayout { get; private set; }

        private readonly ResourceLayoutElementDescription[] _elements;

        internal ShaderUniformLayout(List<ResourceLayoutElementDescription> elements, List<(string name, int resourceIndex, uint offset)> uniforms, List<(int resourceIndex, uint size)> uniformBufferSizes)
        {
            _elements = elements.ToArray();
            _uniformBufferSizes = uniformBufferSizes.ToDictionary(x => x.resourceIndex, x => x.size);

            _uniformValueIndexOffsetByParamName = uniforms.ToDictionary(x => x.name, x => (x.resourceIndex, x.offset));

            _resourceIndexByParamName = new();

            for (int i = 0; i < _elements.Length; i++)
                _resourceIndexByParamName[_elements[i].Name] = i;
        }

        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            ResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(_elements));
        }

        private int GetResourceIndex(string paramName)
        {
            if (_resourceIndexByParamName.TryGetValue(paramName, out int resourceIndex))
                return resourceIndex;
            else
                throw new ArgumentException($"The uniform name {paramName} is not used in this {nameof(ShaderUniformLayout)}");
        }

        private uint GetUniformBufferSize(int resourceIndex)
        {
            if (_uniformBufferSizes.TryGetValue(resourceIndex, out uint size))
                return size;
            else
                throw new ArgumentException($"The resource at {resourceIndex} is not a {nameof(ResourceKind.UniformBuffer)} or has no provided buffer layout");
        }

        private (int index, uint offset) GetUniformValueInfo(string uniformName)
        {
            if (_uniformValueIndexOffsetByParamName.TryGetValue(uniformName, out (int index, uint offset) info))
                return info;
            else
                throw new ArgumentException($"The uniform {uniformName} is not used in this {nameof(ShaderUniformLayout)}");
        }

        /// <summary>
        /// Creates a <see cref="ResourceSet"/> for this <see cref="ShaderUniformLayout"/>
        /// from the given resources
        /// </summary>
        /// <param name="uniformResources">An array of uniform->resource mappings
        /// </param>
        public ResourceSet CreateResourceSet(params (string uniformName, BindableResource value)[] uniformResources)
        {
            BindableResource[] resources = new BindableResource[uniformResources.Length];

            foreach ((string uniformName, BindableResource resource) in uniformResources)
            {
                int resIndex = GetResourceIndex(uniformName);

                resources[resIndex] = resource;
            }

            return CreateResourceSetCore(resources);
        }


        /// <summary>
        /// Creates a <see cref="ResourceSet"/> for this <see cref="ShaderUniformLayout"/>
        /// from the given uniform values
        /// </summary>
        /// <param name="uniformValues">An array of uniform->value mappings
        ///                       <para>value can either be a <see cref="BindableResource"/> or an <see langword="unmanaged"/> value type
        ///                       depending on how it was specified in the <see cref="ShaderUniformLayoutBuilder"/> (and in the shader)</para>
        /// </param>
        public ResourceSet CreateResourceSet(params (string uniformName, object value)[] uniformValues)
        {
            BindableResource[] resources = new BindableResource[uniformValues.Length];

            Dictionary<int, List<(uint offset, object value, string name)>> valuesByResourceIndex = new();

            foreach ((string uniformName, object value) in uniformValues)
            {
                if (value is BindableResource resource)
                {
                    int resIndex = GetResourceIndex(uniformName);

                    if(resources[resIndex]!=null)
                        throw new ArgumentException("Duplicate uniform name " + uniformName);

                    resources[resIndex] = resource;
                }
                else
                {
                    var (resIndex, offset) = GetUniformValueInfo(uniformName);

                    var list = valuesByResourceIndex.GetOrCreate(resIndex);

                    if (list.Any(x => x.offset == offset))
                        throw new ArgumentException("Duplicate uniform name " + uniformName);

                    list.Add((offset, value, uniformName));
                }
            }

            foreach (var (resIndex, list) in valuesByResourceIndex)
            {
                if (resources[resIndex] != null)
                    throw new ArgumentException($"{nameof(ResourceKind.UniformBuffer)} for uniform {list[0].name} was already provided");

                uint size = GetUniformBufferSize(resIndex);

                byte[] buffer = new byte[size];

                foreach (var (offset, value, _) in list)
                {
                    var bufferWriter = UniformTypeCache.GetBufferWriter(value.GetType());

                    bufferWriter.Invoke(buffer, offset, value);
                }

                Debug.Assert(GraphicsAPI.ResourceFactory != null);
                Debug.Assert(GraphicsAPI.GD != null);

                var uniformBuffer = GraphicsAPI.ResourceFactory.CreateBuffer(new BufferDescription(VeldridUtils.GetMinimumBufferSize(size), BufferUsage.UniformBuffer));

                GraphicsAPI.GD.UpdateBuffer(uniformBuffer, 0, buffer);


                resources[resIndex] = uniformBuffer;
            }


            return CreateResourceSetCore(resources);
        }


        /// <summary>
        /// Creates a <see cref="ResourceSet"/> for this <see cref="ShaderUniformLayout"/>
        /// from the given resources
        /// </summary>
        /// <param name="setIndex">the uniform set index/slot in this <see cref="GenericModelRenderer{TIndex, TVertex}"/> to create a ResourceSet for</param>
        /// <param name="uniformResources">An array of uniform->resource mappings
        /// </param>
        public ResourceSet CreateResourceSet(params BindableResource[] resources)
        {
            return CreateResourceSetCore(resources);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ResourceSet CreateResourceSetCore(params BindableResource[] resources)
        {
            EnsureResourcesCreated();

            Debug.Assert(GraphicsAPI.ResourceFactory != null);
            return GraphicsAPI.ResourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayout, resources));
        }
    }
}