using Game.Core;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class StartingResources : IStartable
    {
        private readonly IResourceService _resources;
        private readonly ResourceCost[] _amounts;

        public StartingResources(IResourceService resources, ResourceCost[] amounts)
        {
            _resources = resources;
            _amounts = amounts;
        }

        public void Start()
        {
            if (_amounts == null)
            {
                return;
            }

            foreach (var entry in _amounts)
            {
                if (entry.Amount > 0)
                {
                    _resources.Add(entry.Type, entry.Amount);
                }
            }
        }
    }
}
