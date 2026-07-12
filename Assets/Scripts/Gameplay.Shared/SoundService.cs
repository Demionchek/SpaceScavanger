using Game.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Gameplay.Shared
{
    public sealed class SoundService : ISoundService
    {
        private const float MinDistance = 3f;
        private const float MaxDistance = 25f;

        public void PlayAtPosition(AudioClip clip, Vector3 position, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null)
            {
                return;
            }

            var go = new GameObject($"SFX_{clip.name}");
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.outputAudioMixerGroup = mixerGroup;
            source.Play();

            Object.Destroy(go, clip.length);
        }
    }
}
