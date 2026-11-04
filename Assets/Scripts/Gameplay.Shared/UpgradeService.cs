using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class UpgradeService : IUpgradeService, ISaveable
    {
        private readonly IItemService _items;
        private readonly IShipStatsService _stats;
        private readonly SaveAssetRegistry _registry;
        private readonly List<UpgradeItemDefinition> _installed = new();

        public UpgradeService(IItemService items, IShipStatsService stats, SaveAssetRegistry registry)
        {
            _items = items;
            _stats = stats;
            _registry = registry;
        }

        public IReadOnlyList<UpgradeItemDefinition> Installed => _installed;

        public bool IsInstalled(UpgradeItemDefinition upgrade) =>
            upgrade != null && _installed.Contains(upgrade);

        // Улучшение ставится один раз: второй экземпляр сложил бы модификаторы заново.
        public bool TryInstall(UpgradeItemDefinition upgrade)
        {
            if (upgrade == null || IsInstalled(upgrade) || !_items.TrySpend(upgrade, 1))
            {
                return false;
            }

            _stats.ApplyModifiers(upgrade.Modifiers);
            _installed.Add(upgrade);
            return true;
        }

        public string SaveId => "upgrades";

        public string Save()
        {
            var data = new SaveData();
            foreach (var upgrade in _installed)
            {
                data.installed.Add(_registry.GetId(upgrade));
            }

            return JsonUtility.ToJson(data);
        }

        // Модификаторы заново не применяются: их состояние держит ShipStatsService,
        // здесь восстанавливается только список установленного.
        public void Load(string json)
        {
            _installed.Clear();

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            foreach (var id in data.installed)
            {
                if (_registry.GetItem(id) is UpgradeItemDefinition upgrade)
                {
                    _installed.Add(upgrade);
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> installed = new();
        }
    }
}
