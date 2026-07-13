namespace Game.Core
{
    public readonly struct DialogueRequestedEvent
    {
        public readonly string Node;

        public DialogueRequestedEvent(string node)
        {
            Node = node;
        }
    }
}
