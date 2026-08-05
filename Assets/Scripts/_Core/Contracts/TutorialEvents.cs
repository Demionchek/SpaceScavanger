namespace Game.Core
{
    public readonly struct TutorialPopupEvent
    {
        public readonly string Message;
        public readonly bool Show;

        public TutorialPopupEvent(string message, bool show)
        {
            Message = message;
            Show = show;
        }
    }

    public readonly struct SalvageExtractedEvent
    {
    }
}
