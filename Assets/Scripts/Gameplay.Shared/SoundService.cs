using Game.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Gameplay.Shared
{
    public sealed class SoundService : ISoundService
    {
        private const float MinDistance = 3f;
        private const float MaxDistance = 25f;

        private AudioListener _listener;

        public void PlayAtPosition(AudioClip clip, Vector3 position, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null)
            {
                return;
            }

            var go = new GameObject($"SFX_{clip.name}");

            // Слушатель висит на камере, а та стоит в z = -10. Источник ставится в
            // его плоскость, поэтому расстояние считается только по XY — то же, что
            // видит игрок. Иначе выстрел у самого носа корабля уже считался
            // удалённым на десять единиц и терял треть громкости, а дальний край
            // слышимости (25) съедался до 23 по горизонтали.
            go.transform.position = new Vector3(position.x, position.y, ListenerZ(position.z));

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

        public void Play(AudioClip clip, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null)
            {
                return;
            }

            var go = new GameObject($"SFX_{clip.name}");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = mixerGroup;
            source.Play();

            Object.Destroy(go, clip.length);
        }

        private float ListenerZ(float fallback)
        {
            if (_listener == null)
            {
                _listener = Object.FindFirstObjectByType<AudioListener>();
            }

            return _listener != null ? _listener.transform.position.z : fallback;
        }
    }
}
