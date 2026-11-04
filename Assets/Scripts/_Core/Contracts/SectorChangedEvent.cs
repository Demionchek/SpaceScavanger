namespace Game.Core
{
    public readonly struct SectorChangedEvent
    {
        public readonly int Number;

        public SectorChangedEvent(int number)
        {
            Number = number;
        }
    }
}
