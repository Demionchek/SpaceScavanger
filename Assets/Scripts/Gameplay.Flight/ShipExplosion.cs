using Game.Core;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Враг рвётся тем же эффектом, что и игрок. Health сам уничтожает объект сразу
    // после Died, поэтому эффект ставится без родителя — иначе он исчез бы вместе
    // с тем, кто взорвался.
    [RequireComponent(typeof(Health))]
    public sealed class ShipExplosion : MonoBehaviour
    {
        private ISoundService _sound;
        private ExplosionKit _kit;
        private Health _health;

        [Inject]
        public void Construct(ISoundService sound, ExplosionKit kit)
        {
            _sound = sound;
            _kit = kit;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void OnDied()
        {
            if (_kit == null)
            {
                return;
            }

            if (_kit.Effect != null)
            {
                var effect = Instantiate(_kit.Effect, transform.position, Quaternion.identity);

                // Эффект нарисован под корабль игрока, враги мельче.
                effect.transform.localScale = Vector3.one * Mathf.Max(0.05f, _kit.EnemyScale);
                Destroy(effect, Mathf.Max(0.1f, _kit.Lifetime));
            }

            _sound?.PlayAtPosition(_kit.RandomClip(), transform.position, _kit.MixerGroup);
        }
    }
}
