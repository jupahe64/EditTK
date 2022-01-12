using System;

namespace EditTK.Graphics.Helpers
{
    /// <summary>
    /// Represents the source of a shader
    /// <para>the source can be reloaded/updated see <see cref="Update(byte[])"/></para>
    /// </summary>
    public class ShaderSource
    {
        public event Action? Updated;

        public ShaderSource(byte[] shaderBytes)
        {
            ShaderBytes = shaderBytes;
        }

        public byte[] ShaderBytes { get; private set; }

        public void Update(byte[] shaderBytes)
        {
            ShaderBytes = shaderBytes;
            Updated?.Invoke();
        }

        public static implicit operator ShaderSource(byte[] bytes) => new(bytes);
    }
}