using System.Numerics;
using Veldrid;

namespace EditTK.Graphics.Rendering
{
    //TODO is this all needed?

    public interface IDrawable
    {
        void CreateGraphicsResources(ResourceFactory factory);
        void DestroyGraphicsResources();
        void Draw(CommandList cl, ResourceSet sceneParamsSet, Pass pass);
        void UpdateHighlight(Vector4 highlight);
        bool HasHighlight();
    }

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