namespace Game.Core
{
    public readonly struct CutsceneStartedEvent
    {
        public readonly string SceneName;

        public CutsceneStartedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    public readonly struct CutsceneFinishedEvent
    {
        public readonly string SceneName;

        public CutsceneFinishedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
