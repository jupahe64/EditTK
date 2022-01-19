using System;

namespace EditTK.Graphics.Helpers
{
    public abstract class UpdateableResource<TResource>
    {
        public event Action? Updated;

        public UpdateableResource(TResource resource)
        {
            Resource = resource;
        }

        public TResource Resource { get; private set; }

        public void Update(TResource resource)
        {
            Resource = resource;
            Updated?.Invoke();
        }
    }
}