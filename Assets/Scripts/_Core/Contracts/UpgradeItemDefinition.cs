using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Item/Upgrade Item", fileName = "UpgradeItem")]
    public sealed class UpgradeItemDefinition : ItemDefinition
    {
        public ShipStatModifier[] Modifiers;
    }
}
