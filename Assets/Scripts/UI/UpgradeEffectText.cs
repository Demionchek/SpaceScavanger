using System.Text;
using Game.Core;
using UnityEngine;

namespace Game.UI
{
    // Короткая расшифровка модификаторов улучшения: «Hook +1», «Hull ×1.2».
    // Нужна и верстаку, и экрану состояния, поэтому лежит отдельно.
    public static class UpgradeEffectText
    {
        private static readonly StringBuilder Builder = new();

        public static string Describe(UpgradeItemDefinition upgrade)
        {
            if (upgrade == null || upgrade.Modifiers == null)
            {
                return string.Empty;
            }

            Builder.Clear();

            foreach (var modifier in upgrade.Modifiers)
            {
                var scales = modifier.Multiplier > 0f && !Mathf.Approximately(modifier.Multiplier, 1f);
                if (!scales && modifier.FlatBonus == 0)
                {
                    continue;
                }

                if (Builder.Length > 0)
                {
                    Builder.Append(", ");
                }

                Builder.Append(Label(modifier.Stat));

                if (scales)
                {
                    Builder.Append($" ×{modifier.Multiplier:0.##}");
                }

                if (modifier.FlatBonus != 0)
                {
                    Builder.Append(modifier.FlatBonus > 0 ? $" +{modifier.FlatBonus}" : $" {modifier.FlatBonus}");
                }
            }

            return Builder.ToString();
        }

        public static string Label(ShipStat stat) => stat switch
        {
            ShipStat.ThrustForce => "Thrust",
            ShipStat.Damage => "Damage",
            ShipStat.FireRate => "Fire rate",
            ShipStat.HookLevel => "Hook",
            ShipStat.MaxHealth => "Hull",
            ShipStat.RadarRange => "Radar",
            _ => stat.ToString()
        };
    }
}
