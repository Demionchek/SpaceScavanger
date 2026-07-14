using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class UpgradeService : IUpgradeService
    {
        private readonly IItemService _items;
        private readonly IShipStatsService _stats;
        private readonly List<UpgradeItemDefinition> _installed = new();

        public UpgradeService(IItemService items, IShipStatsService stats)
        {
            _items = items;
            _stats = stats;
        }

        public IReadOnlyList<UpgradeItemDefinition> Installed => _installed;

        public bool TryInstall(UpgradeItemDefinition upgrade)
        {
            if (upgrade == null || !_items.TrySpend(upgrade, 1))
            {
                return false;
            }

            _stats.ApplyModifiers(upgrade.Modifiers);
            _installed.Add(upgrade);
            return true;
        }
    }
}
