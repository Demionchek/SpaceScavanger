namespace Game.Core
{
    public readonly struct HudVisibilityChangedEvent
    {
        public readonly bool Visible;

        public HudVisibilityChangedEvent(bool visible)
        {
            Visible = visible;
        }
    }
}
