using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    // Взрыв корабля — один набор на всех: игрок и враги рвутся одинаково, только
    // игроку он звучит в 2D, а врагу с позиции. Живёт на FlightInstaller, чтобы
    // четыре префаба врагов не носили четыре копии одних и тех же ссылок.
    public sealed class ExplosionKit
    {
        public ExplosionKit(GameObject effect, AudioClip[] clips, AudioMixerGroup mixerGroup,
            float lifetime, float enemyScale)
        {
            Effect = effect;
            Clips = clips;
            MixerGroup = mixerGroup;
            Lifetime = lifetime;
            EnemyScale = enemyScale;
        }

        public GameObject Effect { get; }
        public AudioClip[] Clips { get; }
        public AudioMixerGroup MixerGroup { get; }
        public float Lifetime { get; }
        public float EnemyScale { get; }

        public AudioClip RandomClip() =>
            Clips == null || Clips.Length == 0 ? null : Clips[Random.Range(0, Clips.Length)];
    }
}
