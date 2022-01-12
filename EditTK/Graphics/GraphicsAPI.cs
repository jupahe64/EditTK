using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;

namespace EditTK.Graphics
{
    /// <summary>
    /// Keeps track of the current <see cref="GraphicsDevice"/> (see <see cref="GD"/>) and it's Resources
    /// </summary>
    public static class GraphicsAPI
    {
        private static readonly ManualResetEvent s_gdCanBeChanged = new(true);

        private static DisposeCollectorResourceFactory? s_factory;

        private static GraphicsDevice? s_gd;

        private static List<ResourceHolder> s_resourceHolders = new();

        public static ResourceFactory? ResourceFactory => s_factory;
        public static GraphicsDevice? GD => s_gd;

        internal static void SetGraphicsDevice(GraphicsDevice? gd)
        {
            s_gdCanBeChanged.WaitOne();

            if (s_gd == gd)
                return;

            s_factory?.DisposeCollector.DisposeAll();

            s_gd?.Dispose();

            s_gd = gd;

            foreach (var resource in s_resourceHolders)
            {
                resource.InvalidateResources();
            }

            if (s_gd == null)
                return;

            s_factory = new DisposeCollectorResourceFactory(s_gd.ResourceFactory);



            foreach (var resource in s_resourceHolders)
            {
                resource.EnsureResourcesCreated();
            }
        }

        #region locking

        public sealed class ProtectionLock : IDisposable
        {
            internal ProtectionLock() => AddProtectionLock(this);
            public void Dispose() => RemoveProtectionLock(this);
        }

        private static readonly HashSet<ProtectionLock> _lockOwners = new();

        /// <summary>
        /// Protects the <see cref="GraphicsDevice"/> from changing/destructing until the <see cref="ProtectionLock"/> is disposed
        /// <para>This is only needed if you are using <see cref="GD"/> from a different Thread</para>
        /// </summary>
        /// <returns></returns>
        public static ProtectionLock ProtectGraphicsDevice() => new();

        private static void AddProtectionLock(ProtectionLock projectionLock)
        {
            lock (_lockOwners)
            {
                _lockOwners.Add(projectionLock);
                s_gdCanBeChanged.Reset();
            }
        }

        private static void RemoveProtectionLock(ProtectionLock projectionLock)
        {
            lock (_lockOwners)
            {
                _lockOwners.Remove(projectionLock);

                if(_lockOwners.Count == 0)
                    s_gdCanBeChanged.Set();
            }
        }

        #endregion


        internal static void RegisterResourceHolder(ResourceHolder sharedResource)
        {
            s_resourceHolders.Add(sharedResource);
        }

        internal static void UnregisterResourceHolder(ResourceHolder sharedResource)
        {
            s_resourceHolders.Remove(sharedResource);
        }
    }

    public abstract class ResourceHolder
    {
        private bool _resourcesCreated = false;

        protected ResourceHolder()
        {
            GraphicsAPI.RegisterResourceHolder(this);
        }

        ~ResourceHolder()
        {
            GraphicsAPI.UnregisterResourceHolder(this);
        }

        protected abstract void CreateResources(ResourceFactory factory, GraphicsDevice graphicsDevice);

        public void EnsureResourcesCreated()
        {
            if (_resourcesCreated)
                return;

            Debug.Assert(GraphicsAPI.ResourceFactory!=null);
            Debug.Assert(GraphicsAPI.GD!=null);

            CreateResources(GraphicsAPI.ResourceFactory, GraphicsAPI.GD);

            _resourcesCreated = true;
        }

        protected void UpdateResources()
        {
            Debug.Assert(GraphicsAPI.ResourceFactory != null);
            Debug.Assert(GraphicsAPI.GD != null);

            CreateResources(GraphicsAPI.ResourceFactory, GraphicsAPI.GD);

            _resourcesCreated = true;
        }

        internal void InvalidateResources()
        {
            _resourcesCreated = false;
        }
    }
}
