using Veldrid;

namespace EditTK.Graphics.Common
{
    /// <summary>
    /// Represents the source of a shader
    /// <para>the source can be reloaded/updated see <see cref="Update(byte[])"/></para>
    /// </summary>
    public class ShaderSource : UpdateableResource<byte[]>
    {
        public ShaderSource(byte[] shaderBytes)
            : base(shaderBytes)
        {
            
        }

        public byte[] ShaderBytes => Resource;

        public new void Update(byte[] shaderBytes) => base.Update(shaderBytes);

        public static implicit operator ShaderSource(byte[] bytes) => new(bytes);
    }
}