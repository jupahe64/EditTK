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
}