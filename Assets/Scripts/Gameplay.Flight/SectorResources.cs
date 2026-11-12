using System.Collections.Generic;
using Game.Data;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    // Что сектор вообще может дать. Один список и для генератора, и для награды
    // за головоломку: иначе «ресурс этого сектора» в двух местах разошёлся бы.
    public static class SectorResources
    {
        // Жребий сектора: каждый доступный вид лежит в списке столько раз, сколько
        // стоит его вес, поэтому редкий просто реже вытаскивается.
        public static List<int> Lottery(ZoneConfig config, int sector)
        {
            var lottery = new List<int>();
            if (config == null || config.ResourceSpawns == null)
            {
                return lottery;
            }

            for (var i = 0; i < config.ResourceSpawns.Length; i++)
            {
                var entry = config.ResourceSpawns[i];
                if (entry?.Prefab == null || entry.FromSector > sector)
                {
                    continue;
                }

                for (var w = 0; w < entry.Weight; w++)
                {
                    lottery.Add(i);
                }
            }

            return lottery;
        }

        // Обломок-образец: у него спрашивают тип и выход, сам он не спавнится.
        // Уровень крюка здесь не смотрят — вскрытая находка и есть обход крюка.
        public static HookableResource Sample(ZoneConfig config, int sector)
        {
            var lottery = Lottery(config, sector);
            if (lottery.Count == 0)
            {
                return null;
            }

            var prefab = config.ResourceSpawns[lottery[Random.Range(0, lottery.Count)]].Prefab;
            return prefab != null ? prefab.GetComponent<HookableResource>() : null;
        }
    }
}
