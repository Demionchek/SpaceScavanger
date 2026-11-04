namespace Game.Core
{
    public readonly struct RelationshipChangedEvent
    {
        public readonly CharacterDefinition Character;
        public readonly int NewValue;

        public RelationshipChangedEvent(CharacterDefinition character, int newValue)
        {
            Character = character;
            NewValue = newValue;
        }
    }
}
