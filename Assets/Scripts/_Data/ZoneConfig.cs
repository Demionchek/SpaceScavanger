using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Zone/Zone Config", fileName = "ZoneConfig")]
    public sealed class ZoneConfig : ScriptableObject
    {
        public Vector2 AreaSize = new(20f, 20f);

        [Tooltip("Ресурсы сектора: у каждого свой вес в жребии и свой сектор, " +
                 "с которого он начинает попадаться.")]
        public ResourceSpawn[] ResourceSpawns;

        [Tooltip("Сколько кучек разыгрывается за сектор, а не сколько обломков.")]
        public int ResourceCount = 10;
        [Range(0f, 1f)] public float ResourceSpawnChance = 0.7f;

        [Tooltip("Размер кучки: от и до. Тип у всей кучки один — тот, что выпал первому обломку.")]
        public Vector2Int ResourceGroupSize = new(2, 5);

        [Tooltip("В каком радиусе от первого обломка лежит остальная кучка.")]
        public float ResourceGroupRadius = 5f;

        [Tooltip("Свободный радиус между обломками одной кучки: внутри неё тесно.")]
        public float ResourceMemberClearance = 3f;

        public GameObject[] EnemyPrefabs;
        public int EnemyCount = 3;
        [Range(0f, 1f)] public float EnemySpawnChance = 0.3f;

        public GameObject[] TraderPrefabs;
        [Range(0f, 1f)] public float TraderSpawnChance = 0.5f;

        public GameObject[] QuestGiverPrefabs;
        [Tooltip("Сколько квестодателей разыгрывается за сектор. Префаб каждому " +
                 "достаётся свой, поэтому больше, чем префабов, их не будет.")]
        public int QuestGiverCount = 1;
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

        [Header("Puzzles")]
        [Tooltip("Находки на островках: шахта и разбитый спутник. Пусто — не будет.")]
        public GameObject[] PuzzlePrefabs;

        [Tooltip("Сколько находок разыгрывается за сектор. Больше, чем островков " +
                 "и чем самих префабов, их не будет: на островок — одна, и все разные.")]
        public int PuzzleCount = 2;
        [Range(0f, 1f)] public float PuzzleSpawnChance = 0.6f;

        [Tooltip("Насколько находка отходит от кромки породы. Считается от центра " +
                 "крайней клетки (0.64), так что половина отступа уходит в саму клетку.")]
        public float PuzzleIslandMargin = 0.45f;

        [Header("Spacing")]
        [Tooltip("Свободный радиус вокруг кучки — расстояние между кучками.")]
        public float ResourceClearance = 14f;
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
