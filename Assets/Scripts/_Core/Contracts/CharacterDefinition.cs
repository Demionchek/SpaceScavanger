using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Npc/Character", fileName = "Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string DisplayName;

        [Tooltip("Имя говорящего в Yarn-строке, по нему же ищется портрет в SpeakerIconSet.")]
        public string SpeakerName;

        public Sprite Icon;

        [Tooltip("Фракция для общей репутации. Личные отношения от неё не зависят.")]
        public NpcGroup Faction;
    }
}
