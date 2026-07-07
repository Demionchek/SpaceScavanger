using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Item/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string DisplayName;
        public Sprite Icon;
    }
}
