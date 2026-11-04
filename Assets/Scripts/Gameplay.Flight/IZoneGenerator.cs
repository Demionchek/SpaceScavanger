using Game.Data;

namespace Game.Gameplay.Flight
{
    public interface IZoneGenerator
    {
        ZoneContent Generate(ZoneConfig config, ZoneTier tier, int seed);
    }
}
