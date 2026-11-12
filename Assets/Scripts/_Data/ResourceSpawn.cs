using System;
using UnityEngine;

namespace Game.Data
{
    // Один вид ресурса в жребии сектора. Редкость — это вес и сектор, с которого
    // ресурс вообще начинает попадаться: отдельный список на каждый тир пришлось бы
    // держать в согласии с квотой руками.
    [Serializable]
    public sealed class ResourceSpawn
    {
        public GameObject Prefab;

        [Tooltip("С какого сектора ресурс начинает попадаться.")]
        public int FromSector = 1;

        [Tooltip("Вес в жребии среди доступных: 4 вчетверо чаще, чем 1. 0 — не попадается.")]
        public int Weight = 1;
    }
}
