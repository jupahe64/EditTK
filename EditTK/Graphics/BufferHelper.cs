using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Safe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Graphics
{
    public record struct BufferRange(BufferPtr Buffer, ulong Offset, ulong Size);

    public static class BufferHelper
    {
        public unsafe static BufferRange CreateBufferWithData<T>(DevicePtr device, BufferUsage usage, ReadOnlySpan<T> data, string? label = null)
            where T : unmanaged
        {
            var bufferSize = (ulong)(sizeof(T) * data.Length);

            var buffer = device.CreateBuffer(usage,
                bufferSize,
                mappedAtCreation: true, label);

            var mappedBuffer = buffer.GetMappedRange<T>(0, (nuint)bufferSize);
            data.CopyTo(mappedBuffer);

            buffer.Unmap();
            return new(buffer, 0, bufferSize);
        }

        public unsafe static BufferRange CreateBufferWithData<T>(DevicePtr device, BufferUsage usage, in T data, string? label = null)
            where T : unmanaged
        {
            var bufferSize = (ulong)sizeof(T);

            var buffer = device.CreateBuffer(usage,
                bufferSize,
                mappedAtCreation: true, label);

            var mappedBuffer = buffer.GetMappedRange<T>(0, (nuint)bufferSize);
            mappedBuffer[0] = data;

            buffer.Unmap();
            return new(buffer, 0, bufferSize);
        }
    }
}
