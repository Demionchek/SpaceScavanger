namespace Game.Core
{
    public readonly struct DialogueStartedEvent
    {
        public readonly string Node;

        public DialogueStartedEvent(string node)
        {
            Node = node;
        }
    }
}
