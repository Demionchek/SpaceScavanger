namespace Game.Core
{
    public readonly struct DialogueFinishedEvent
    {
        public readonly string Node;

        public DialogueFinishedEvent(string node)
        {
            Node = node;
        }
    }
}
