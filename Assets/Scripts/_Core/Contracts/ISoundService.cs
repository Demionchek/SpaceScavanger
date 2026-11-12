using UnityEngine;
using UnityEngine.Audio;

namespace Game.Core
{
    public interface ISoundService
    {
        void PlayAtPosition(AudioClip clip, Vector3 position, AudioMixerGroup mixerGroup = null);

        // Без позиции: то, что случилось с самим игроком, звучит одинаково громко,
        // где бы он ни был.
        void Play(AudioClip clip, AudioMixerGroup mixerGroup = null);
    }
}
