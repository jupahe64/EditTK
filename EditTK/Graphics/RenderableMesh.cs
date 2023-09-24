using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Safe;
using Safe = Silk.NET.WebGPU.Safe;
using EditTK.Utils;
using System.Drawing;
using System.Reflection.Emit;

namespace EditTK.Graphics
{
    public enum AttributeShaderLoc : ushort
    {
        Loc0 = 1,
        Loc1 = 2,
        Loc2 = 4,
        Loc3 = 8,
        Loc4 = 16,
        Loc5 = 32,
        Loc6 = 64,
        Loc7 = 128,
        Loc8 = 256,
        Loc9 = 512,
        Loc10 = 1024,
        Loc11 = 2048,
        Loc12 = 4096,
        Loc13 = 8192,
        Loc14 = 16384,
        Loc15 = 32768
    }


    [AttributeUsage(AttributeTargets.Field)]
    public class VertexAttributeAttribute : Attribute
    {
        public readonly AttributeShaderLoc ShaderLocMapping;
        public readonly VertexFormat Format;

        public VertexAttributeAttribute(AttributeShaderLoc shaderLocMapping, VertexFormat format)
        {
            ShaderLocMapping = shaderLocMapping;
            Format = format;
        }
    }

    public class VertexStructDescription
    {
        public string Name { get; private set; }
        public IReadOnlyList<(string name, Type type, VertexAttributeAttribute attribute)> Fields => _fields;

        private readonly (string name, Type type, VertexAttributeAttribute attribute)[] _fields;

        public VertexStructDescription(string name, params (string name, Type type, VertexAttributeAttribute description)[] fields)
        {
            Name = name;
            _fields = fields;
        }

        private static readonly Dictionary<Type, VertexStructDescription> _cache = new();

        public static VertexStructDescription From<TVertex>() where TVertex : unmanaged
        {
            Type type = typeof(TVertex);

            return _cache.GetOrCreate(type, () =>
            {
                var fieldInfos = type.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

                int targetSize = StructUtil.SizeOf(type);
                int gotSize = 0;


                var fields = new (string name, Type type, VertexAttributeAttribute attribute)[fieldInfos.Length];

                int i = 0;

                foreach (var fieldInfo in fieldInfos)
                {
                    VertexAttributeAttribute? attribute = (VertexAttributeAttribute?)fieldInfo.GetCustomAttributes(typeof(VertexAttributeAttribute), true).FirstOrDefault();

                    if (attribute == null)
                        throw new ArgumentException($"Field {fieldInfo.Name} of vertex struct {type.Name} has no VertexAttribute-Attribute");

                    gotSize += StructUtil.SizeOf(fieldInfo.FieldType);

                    fields[i++] = (fieldInfo.Name, fieldInfo.FieldType, attribute);
                }

                if (gotSize != targetSize)
                    Debugger.Break(); //we didn't get all fields or something else went wrong

                return new(type.Name, fields);
            });
        }
    }

    public record struct TriangleU16(ushort IndexA, ushort IndexB, ushort IndexC);
    public record struct TriangleU32(uint IndexA, uint IndexB, uint IndexC);

    public class RenderableMesh
    {
        private readonly (BufferRange buffer, VertexStructDescription vertexStructDesc)?[] _vertexBufferInfos;
        private uint IndexCount { get; set; }
        private readonly (IndexFormat, BufferRange)? _indexBuffer;

        internal bool OwnsBuffers { private get; init; } = false;

        public static RenderableMesh Create<TVertex>(DevicePtr device, ReadOnlySpan<TVertex> vertices, string? label = null)
            where TVertex : unmanaged
        {
            VertexStructDescription description = VertexStructDescription.From<TVertex>();

            BufferRange vertexBuffer = BufferHelper.CreateBufferWithData(device, BufferUsage.Vertex, vertices,
                label == null ? null :$"{label} - Vertexbuffer");

            return new((uint)vertices.Length, null, (vertexBuffer, description))
            {
                OwnsBuffers = true
            };
        }

        public static RenderableMesh Create<TIndex, TVertex>(DevicePtr device, 
            ReadOnlySpan<TIndex> indices, ReadOnlySpan<TVertex> vertices, string? label = null)
            where TVertex : unmanaged
            where TIndex : unmanaged
        {
            VertexStructDescription description = VertexStructDescription.From<TVertex>();

            var drawElementsType = GetIndexFormat<TIndex>(out int indicesPerElement);

            BufferRange indexBuffer = BufferHelper.CreateBufferWithData(device, BufferUsage.Index, indices,
                label == null ? null : $"{label} - IndexBuffer");

            BufferRange vertexBuffer = BufferHelper.CreateBufferWithData(device, BufferUsage.Vertex, vertices,
                label == null ? null : $"{label} - Vertexbuffer");

            return new((uint)(indices.Length * indicesPerElement), (drawElementsType, indexBuffer), (vertexBuffer, description))
            {
                OwnsBuffers = true
            };
        }

        private RenderableMesh(uint elementCount, (IndexFormat indexType, BufferRange buffer)? indexBuffer,
            params (BufferRange buffer, VertexStructDescription vertexStructDesc)?[] vertexBufferInfos)
        {
            _vertexBufferInfos = vertexBufferInfos;
            IndexCount = elementCount;
            _indexBuffer = indexBuffer;
        }

        public unsafe void Draw(RenderPassEncoderPtr pass,
            uint firstInstance = 0, uint instanceCount = 1, (uint start, uint count)? customIndexRange = null)
        {
            var indexRange = customIndexRange ?? (0, IndexCount);

            if (instanceCount == 0)
                return;

            if (_indexBuffer is (IndexFormat format, BufferRange ib))
                pass.SetIndexBuffer(ib.Buffer, format, ib.Offset, ib.Size);


            for (uint i = 0; i < _vertexBufferInfos.Length; i++)
            {
                if (_vertexBufferInfos[i] is not (BufferRange vb, _))
                    continue;

                pass.SetVertexBuffer(i, vb.Buffer, vb.Offset, vb.Size);
            }

            if (_indexBuffer.HasValue)
                pass.DrawIndexed(indexRange.count, instanceCount, indexRange.start, 0, firstInstance);
            else
                pass.Draw(indexRange.count, instanceCount, indexRange.start, firstInstance);
        }

        public void CleanUp()
        {
            if (!OwnsBuffers) return;

            for (int iBuffer = 0; iBuffer < _vertexBufferInfos.Length; iBuffer++)
            {
                if (_vertexBufferInfos[iBuffer] is not (BufferRange vb, _))
                    continue;

                vb.Buffer.Destroy();
            }

            if(_indexBuffer is (_, BufferRange ib))
                ib.Buffer.Destroy();
        }

        #region Helper functions
        private static IndexFormat GetIndexFormat<TIndex>(out int indicesPerElement)
        {
            var indexType = typeof(TIndex);

            indicesPerElement = 1;

            IndexFormat format;

            if (indexType == typeof(ushort))
            {
                format = IndexFormat.Uint16;
            }
            else if (indexType == typeof(uint))
            {
                format = IndexFormat.Uint32;
            }
            else if (indexType == typeof(TriangleU16))
            {
                format = IndexFormat.Uint16;
                indicesPerElement = 3;
            }
            else if (indexType == typeof(TriangleU32))
            {
                format = IndexFormat.Uint32;
                indicesPerElement = 3;
            }
            else
                throw new ArgumentException($"Index type has to be either byte, ushort or uint, was {typeof(TIndex).Name}");

            return format;
        }

        public Safe.VertexState CreateVertexState((string entryPoint, ShaderModulePtr module) vertexShader, (string, double)[] shaderConstants)
        {
            var bufferLayouts = new Safe.VertexBufferLayout[_vertexBufferInfos.Length];
            ushort shaderLocAssignments = 0;

            for (int iBuffer = 0; iBuffer < _vertexBufferInfos.Length; iBuffer++)
            {
                if (_vertexBufferInfos[iBuffer] is not var (vtxBuffer, vtxStructDesc))
                    continue;

                uint offset = 0;

                var vertexAttributes = new List<VertexAttribute>();

                foreach (var (name, type, attribute) in vtxStructDesc.Fields)
                {
                    ushort bitField = (ushort)attribute.ShaderLocMapping;

                    if ((bitField & shaderLocAssignments) != 0)
                        throw new InvalidOperationException($"Shader location(s) of {name} overlap with atleast 1 other attribute");

                    for (int iLocation = 0; iLocation < 16; iLocation++)
                    {
                        if (((bitField >> iLocation) & 1) != 1)
                            continue;

                        vertexAttributes.Add(new VertexAttribute()
                        {
                            ShaderLocation = (ushort)iLocation,
                            Format = attribute.Format,
                            Offset = offset
                        });
                    }
                    
                    offset += (uint)StructUtil.SizeOf(type);

                    shaderLocAssignments |= bitField;
                }

                var totalSize = offset;

                bufferLayouts[iBuffer] = new Safe.VertexBufferLayout
                {
                    ArrayStride = totalSize,
                    Attributes = vertexAttributes.ToArray(),
                    StepMode = VertexStepMode.Vertex
                };
            }

            return new Safe.VertexState
            {
                Module = vertexShader.module,
                EntryPoint = vertexShader.entryPoint,
                Constants = shaderConstants,
                Buffers = bufferLayouts
            };
        }
        #endregion
    }
}
