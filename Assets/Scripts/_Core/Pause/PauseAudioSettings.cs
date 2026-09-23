using System;
using UnityEngine.Audio;

namespace Game.Core
{
    [Serializable]
    public sealed class PauseAudioSettings
    {
        public AudioMixerSnapshot GameplaySnapshot;
        public AudioMixerSnapshot PausedSnapshot;
        public float TransitionTime = 0.3f;

        public bool IsConfigured => GameplaySnapshot != null && PausedSnapshot != null;
    }
}
