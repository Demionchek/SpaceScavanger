namespace Game.Core
{
    public readonly struct RaceRequestedEvent
    {
        public readonly RaceDefinition Race;

        public RaceRequestedEvent(RaceDefinition race)
        {
            Race = race;
        }
    }

    public readonly struct RaceCountdownEvent
    {
        public readonly int Seconds;

        public RaceCountdownEvent(int seconds)
        {
            Seconds = seconds;
        }
    }

    public readonly struct RaceStartedEvent
    {
        public readonly int CheckpointCount;

        public RaceStartedEvent(int checkpointCount)
        {
            CheckpointCount = checkpointCount;
        }
    }

    public readonly struct RaceProgressEvent
    {
        public readonly int NextCheckpoint;
        public readonly int CheckpointCount;

        public RaceProgressEvent(int nextCheckpoint, int checkpointCount)
        {
            NextCheckpoint = nextCheckpoint;
            CheckpointCount = checkpointCount;
        }
    }

    public readonly struct RaceFinishedEvent
    {
        public readonly int Place;
        public readonly int Participants;

        public RaceFinishedEvent(int place, int participants)
        {
            Place = place;
            Participants = participants;
        }
    }
}
