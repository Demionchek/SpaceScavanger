using UnityEngine;
using UnityEngine.Audio;

namespace Game.Core
{
    public interface ISoundService
    {
        void PlayAtPosition(AudioClip clip, Vector3 position, AudioMixerGroup mixerGroup = null);
    }
}
