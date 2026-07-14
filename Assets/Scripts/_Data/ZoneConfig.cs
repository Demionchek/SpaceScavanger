using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Zone/Zone Config", fileName = "ZoneConfig")]
    public sealed class ZoneConfig : ScriptableObject
    {
        public Vector2 AreaSize = new(20f, 20f);

        public GameObject[] ResourcePrefabs;
        public int ResourceCount = 10;
        [Range(0f, 1f)] public float ResourceSpawnChance = 0.7f;

        public GameObject[] EnemyPrefabs;
        public int EnemyCount = 3;
        [Range(0f, 1f)] public float EnemySpawnChance = 0.3f;

        public GameObject[] TraderPrefabs;
        [Range(0f, 1f)] public float TraderSpawnChance = 0.5f;

        public GameObject[] QuestGiverPrefabs;
        [Range(0f, 1f)] public float QuestGiverSpawnChance = 0.5f;
    }
}
