using UnityEngine;

namespace Game.Gameplay.Flight
{
    public enum ZoneItemKind
    {
        Resource,
        Enemy,
        Trader,
        QuestGiver,
        Wormhole,
        Island
    }

    public sealed class ZoneItemTag : MonoBehaviour
    {
        public ZoneItemKind Kind;
        public int PrefabIndex;
    }
}
