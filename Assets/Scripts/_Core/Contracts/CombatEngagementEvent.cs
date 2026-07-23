namespace Game.Core
{
    // Враг вступил в бой (Engaged=true) или выбыл/уничтожен (false). Считается
    // CombatStateTracker'ом для запрета входа в интерьер во время боя.
    public readonly struct CombatEngagementEvent
    {
        public readonly bool Engaged;

        public CombatEngagementEvent(bool engaged)
        {
            Engaged = engaged;
        }
    }
}
