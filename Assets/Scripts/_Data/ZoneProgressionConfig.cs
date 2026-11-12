using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class ZoneTier
    {
        [Tooltip("С какого сектора действует тир. Берётся последний подходящий, " +
                 "поэтому тиры должны идти по возрастанию.")]
        public int FromSector = 1;

        [Tooltip("Враги этого тира. Пусто — берутся враги из ZoneConfig.")]
        public GameObject[] EnemyPrefabs;

        [Tooltip("Сколько групп врагов разыгрывается за сектор, а не сколько врагов.")]
        public int EnemyCount = 3;
        [Range(0f, 1f)] public float EnemySpawnChance = 0.75f;

        [Tooltip("Размер группы: от и до. 1 и 1 — одиночки.")]
        public Vector2Int GroupSize = new(1, 1);

        [Tooltip("В каком радиусе от вожака держатся остальные в группе.")]
        public float GroupRadius = 8f;

        [Tooltip("Сколько кучек ресурсов раскидывается по сектору: чем дальше, тем " +
                 "беднее сектор, но квота должна оставаться выполнимой.")]
        public int ResourceCount = 14;

        [Tooltip("Чего стоит прыжок из этого сектора. 0 — оставить цену префаба червоточины.")]
        public int JumpCost;

        public GameObject[] EnemiesOr(ZoneConfig config) =>
            EnemyPrefabs != null && EnemyPrefabs.Length > 0 ? EnemyPrefabs : config.EnemyPrefabs;

        public int RollGroupSize(System.Random random)
        {
            var min = Mathf.Max(1, GroupSize.x);
            var max = Mathf.Max(min, GroupSize.y);
            return random.Next(min, max + 1);
        }
    }

    [CreateAssetMenu(menuName = "Game/Zone/Zone Progression", fileName = "ZoneProgressionConfig")]
    public sealed class ZoneProgressionConfig : ScriptableObject
    {
        [SerializeField] private ZoneTier[] _tiers;

        public ZoneTier GetTier(int sector)
        {
            if (_tiers == null)
            {
                return null;
            }

            ZoneTier result = null;
            foreach (var tier in _tiers)
            {
                if (tier != null && tier.FromSector <= sector)
                {
                    result = tier;
                }
            }

            return result;
        }
    }
}
