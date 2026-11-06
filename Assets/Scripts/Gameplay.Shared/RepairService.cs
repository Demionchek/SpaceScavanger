using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    // Корпус латают в трюме за тот же лом, что уходит в квоту: на всё его не хватает,
    // и этот выбор — часть давления корпорации.
    public sealed class RepairService : IRepairService
    {
        private readonly Health _health;
        private readonly IResourceService _resources;
        private readonly ResourceType _resource;
        private readonly int _hullPerUnit;

        public RepairService(Health health, IResourceService resources, ResourceType resource, int hullPerUnit)
        {
            _health = health;
            _resources = resources;
            _resource = resource;
            _hullPerUnit = Mathf.Max(1, hullPerUnit);
        }

        public ResourceType Resource => _resource;

        public int MissingHull => Mathf.Max(0, Mathf.CeilToInt(_health.MaxHealth - _health.CurrentHealth));

        public int CostToFull => Mathf.CeilToInt(MissingHull / (float)_hullPerUnit);

        // Починка идёт на то, что есть в трюме: неполный ремонт лучше отказа.
        public int AffordableCost => Mathf.Min(CostToFull, _resources.GetAmount(_resource));

        public int RepairableHull => Mathf.Min(AffordableCost * _hullPerUnit, MissingHull);

        public bool CanRepair => AffordableCost > 0;

        public int Repair()
        {
            var units = AffordableCost;
            if (units <= 0 || !_resources.TrySpend(_resource, units))
            {
                return 0;
            }

            var restored = Mathf.Min(units * _hullPerUnit, MissingHull);
            _health.Heal(restored);
            return restored;
        }
    }
}
