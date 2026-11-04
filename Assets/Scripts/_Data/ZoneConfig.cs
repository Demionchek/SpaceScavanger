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

        public GameObject WormholePrefab;

        [Header("Islands")]
        [Tooltip("Наборы породы: светлые и тёмные. Пусто — островков не будет.")]
        public SpaceTileSet[] IslandTileSets;
        public int IslandCount = 3;
        [Range(0f, 1f)] public float IslandSpawnChance = 0.8f;
        [Tooltip("Радиус островка в тайлах (тайл 0.64 ед.): от и до.")]
        public Vector2Int IslandRadiusTiles = new(4, 10);
        [Tooltip("Свободное кольцо вокруг островка сверх его собственного радиуса.")]
        public float IslandMargin = 6f;
        [Tooltip("Слой коллайдера островка: 3 — Ground.")]
        public int IslandLayer = 3;
        public int IslandSortingOrder = -10;
        public int IslandDetailOrder = -9;
        [Range(0f, 1f)] public float IslandDetailChance = 0.25f;

        [Header("Spacing")]
        [Tooltip("Свободный радиус вокруг ресурса, в мировых единицах.")]
        public float ResourceClearance = 6f;
        [Tooltip("Свободный радиус вокруг врага — расстояние между группами.")]
        public float EnemyClearance = 10f;
        [Tooltip("Свободный радиус между соседями по группе: внутри группы тесно.")]
        public float GroupMemberClearance = 4f;
        [Tooltip("Свободный радиус вокруг червоточины, торговца и квестодателя.")]
        public float LandmarkClearance = 16f;
        [Tooltip("Свободный круг в центре зоны: туда игрок попадает после прыжка.")]
        public float PlayerClearRadius = 12f;
        [Tooltip("Сколько точек генератор перебирает, прежде чем сдаться. Ресурс или враг " +
                 "тогда не спавнится, одиночный объект встаёт в самое свободное место.")]
        public int PlacementAttempts = 30;
    }
}
