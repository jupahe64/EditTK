using System;
using Veldrid;

namespace EditTK.Graphics.Common
{
    /// <summary>
    /// Describes how a field in a Vertex struct should be interpreted by shaders
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VertexAttributeAtrribute : Attribute
    {
        internal readonly string AttributeName;
        internal readonly VertexElementFormat AttributeFormat;
        internal readonly int Count;

        /// <param name="attributeName">The name of this attribute in the shader(s)</param>
        /// <param name="attributeFormat">The value type of this attribute </param>
        /// <param name="count">The number of elements this attribute consists of 
        /// <para>For example: <c>mat4</c> consists of 4 <c>vec4s</c>(<see cref="VertexElementFormat.Float4"/>)</para>
        /// </param>
        public VertexAttributeAtrribute(string attributeName, VertexElementFormat attributeFormat, int count = 1)
        {
            if(count < 1) throw new ArgumentOutOfRangeException($"{nameof(count)} must be higher than 0");

            AttributeName = attributeName;
            AttributeFormat = attributeFormat;
            Count = count;
        }
    }
}