using System;
using System.Diagnostics;
using Veldrid;

namespace EditTK.Graphics.Common
{
    /// Holds instance data for instanced rendering using a <see cref="GenericInstanceRenderer{TIndex, TVertex, TInstance}"/>
    /// <typeparam name="TInstance">The instance type used in the InstanceBuffer
    ///                     <para>Note: All fields need to have a <see cref="VertexAttributeAtrribute"/></para>
    public class GenericInstanceHolder<TInstance> : ResourceHolder
        where TInstance : unmanaged
    {
        TInstance[] _instances = Array.Empty<TInstance>();
        private int _instanceCount;

        private int _defaultCapacity = 16;

        private uint _lastUpdateBufferSize = int.MaxValue;

        private DeviceBuffer? _instanceBuffer;
        private DeviceBuffer? _stagingBuffer;

        public DeviceBuffer InstanceBuffer => _instanceBuffer;
        public int Count => _instanceCount;


        //shamelessly stolen from https://referencesource.microsoft.com/#mscorlib/system/collections/generic/list.cs,eb66b6616c6fd4ef
        private void EnsureCapacity(int min)
        {
            if (_instances.Length < min)
            {
                int newCapacity = _instances.Length == 0 ? _defaultCapacity : _instances.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
                if (newCapacity < min) newCapacity = min;

                if (newCapacity > 0)
                {
                    TInstance[] newArray = new TInstance[newCapacity];
                    if (_instanceCount > 0)
                    {
                        Array.Copy(_instances, 0, newArray, 0, _instanceCount);
                    }
                    _instances = newArray;
                }
                else
                {
                    _instances = Array.Empty<TInstance>();
                }
            }
        }

        public void Clear()
        {
            _instanceCount = 0;
        }

        public void Add(TInstance instance)
        {
            EnsureCapacity(_instanceCount+1);

            _instanceCount++;

            _instances[_instanceCount-1] = instance;
        }


        protected override void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice)
        {
            //force invalidate buffers
            _lastUpdateBufferSize = int.MaxValue;
        }

        public unsafe void UpdateInstanceBuffer(CommandList cl)
        {
            Debug.Assert(GraphicsAPI.ResourceFactory != null);

            uint size = VeldridUtils.GetSizeInBytes(_instances);

            uint bufferSize = VeldridUtils.GetMinimumBufferSize(size);

            

            if(_lastUpdateBufferSize!= bufferSize)
            {
                _instanceBuffer?.Dispose();
                _stagingBuffer?.Dispose();

                _instanceBuffer = GraphicsAPI.ResourceFactory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.VertexBuffer));
                _stagingBuffer  = GraphicsAPI.ResourceFactory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.Staging));

                _instanceBuffer.Name = "Instancing";
                _stagingBuffer.Name  = "Instancing Staging";
            }

            _lastUpdateBufferSize = bufferSize;

            
            if(_instanceCount > 0)
            {
                cl.UpdateBuffer(_stagingBuffer, 0, ref _instances[0], size);

                cl.CopyBuffer(_stagingBuffer, 0, _instanceBuffer, 0, size);
            }
        }


    }
}