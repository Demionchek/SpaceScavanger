using UnityEngine;

namespace Game.Gameplay.Flight
{
    public enum ZoneItemKind
    {
        Resource,
        Enemy,
        Trader,
        QuestGiver,
        Wormhole
    }

    public sealed class ZoneItemTag : MonoBehaviour
    {
        public ZoneItemKind Kind;
        public int PrefabIndex;
    }
}
