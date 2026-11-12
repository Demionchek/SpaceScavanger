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
        Island,

        // Только в конец: номер пишется в сейв, и старые сохранения иначе
        // передумают, что в них лежит.
        Puzzle
    }

    public sealed class ZoneItemTag : MonoBehaviour
    {
        public ZoneItemKind Kind;
        public int PrefabIndex;
    }
}
