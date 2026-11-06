using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Item/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        public string DisplayName;
        public Sprite Icon;

        [Tooltip("Зачем предмет нужен. Печатается в верстаке под названием.")]
        [TextArea]
        public string Description;
    }
}
