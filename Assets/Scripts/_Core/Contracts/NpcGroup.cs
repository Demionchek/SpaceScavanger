using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Npc/Npc Group", fileName = "NpcGroup")]
    public sealed class NpcGroup : ScriptableObject
    {
        public string DisplayName;
    }
}
