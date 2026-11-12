using Game.Data;

namespace Game.Gameplay.Flight
{
    public interface IZoneGenerator
    {
        // Номер сектора нужен сам по себе, а не только через тир: от него зависит,
        // какие ресурсы уже попадаются.
        ZoneContent Generate(ZoneConfig config, ZoneTier tier, int sector, int seed);
    }
}
