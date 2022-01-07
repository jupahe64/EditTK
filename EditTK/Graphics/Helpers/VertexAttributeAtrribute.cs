using System;
using Veldrid;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// Describes how a field in a Vertex struct should be interpreted by shaders
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VertexAttributeAtrribute : Attribute
    {
        internal readonly string AttributeName;
        internal readonly VertexElementFormat AttributeFormat;

        /// <param name="attributeName">The name of this attribute in the shader(s)</param>
        /// <param name="attributeFormat">The value type of this attribute </param>
        public VertexAttributeAtrribute(string attributeName, VertexElementFormat attributeFormat)
        {
            AttributeName = attributeName;
            AttributeFormat = attributeFormat;
        }
    }
}