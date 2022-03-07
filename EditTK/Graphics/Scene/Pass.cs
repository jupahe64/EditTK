using System.Numerics;
using Veldrid;

namespace EditTK.Graphics.Scene
{
    /// <summary>
    /// Enum-like object/type representing a RenderPass
    /// </summary>
    public class Pass
    {
        public static readonly Pass OPAQUE = new("OPAQUE");
        public static readonly Pass TRANSPARENT = new("TRANSPARENT");
        public static readonly Pass HIGHLIGHT_ONLY = new("HIGHLIGHT_ONLY");
        public static readonly Pass GIZMOS = new("GIZMOS");

        public Pass(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}