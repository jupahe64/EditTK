using EditTK.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Veldrid;

namespace EditTK.Graphics.Common.Internal
{
    internal static class VertexFormatCache
    {
        private static readonly Dictionary<Type, VertexLayoutDescription> _vertexLayouts = new();

        private static readonly Dictionary<Type, VertexLayoutDescription> _instanceLayouts = new();


        private static VertexLayoutDescription CreateLayoutDescription(Type type, uint instanceStepRate = 0)
        {
            var fieldInfos = type.GetFields();

            int count = 0;

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                var attr = fieldInfos[i].GetCustomAttribute<VertexAttributeAtrribute>();

                if (attr == null)
                    throw new ArgumentException($"Field {fieldInfos[i].Name} of the struct {type.Name} has no {nameof(VertexAttributeAtrribute)}");

                count += attr.Count;
            }


            var elementDescriptions = new VertexElementDescription[count];

            int index = 0;

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                var attr = fieldInfos[i].GetCustomAttribute<VertexAttributeAtrribute>();

                Debug.Assert(attr != null);

                if (attr.Count == 1)
                {
                    elementDescriptions[index++] = new VertexElementDescription(attr.AttributeName, VertexElementSemantic.TextureCoordinate, attr.AttributeFormat);
                }
                else
                {
                    for (int j = 0; j < attr.Count; j++)
                    {
                        elementDescriptions[index++] = new VertexElementDescription(attr.AttributeName, VertexElementSemantic.TextureCoordinate, attr.AttributeFormat);
                    }
                }

            }

            return new VertexLayoutDescription(elementDescriptions) { InstanceStepRate = instanceStepRate };
        }

        public static VertexLayoutDescription GetVertexLayout<TVertex>()
        {
            Type type = typeof(TVertex);

            return _vertexLayouts.GetOrCreate(type, ()
             => CreateLayoutDescription(type)
             );
        }


        public static VertexLayoutDescription GetInstanceLayout<TInstance>()
        {
            Type type = typeof(TInstance);

            return _instanceLayouts.GetOrCreate(type, ()
             => CreateLayoutDescription(type, 1)
             );
        }
    }
}