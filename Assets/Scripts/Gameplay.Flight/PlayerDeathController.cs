using System;
using Game.Core;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // Взрыв корабля и экран поражения следом: игрок должен успеть увидеть взрыв.
    public sealed class PlayerDeathController : IStartable, ITickable, IDisposable
    {
        private const string CombatReason = "Your ship was torn apart";

        private readonly EventBus _eventBus;
        private readonly PlayerMarker _player;
        private readonly Health _health;
        private readonly ISoundService _sound;
        private readonly ExplosionKit _explosions;
        private readonly GameObject _explosionPrefab;
        private readonly float _deathDelay;

        private string _reason;
        private float _timer;
        private bool _pending;

        public PlayerDeathController(EventBus eventBus, PlayerMarker player, Health health,
            ISoundService sound, ExplosionKit explosions, GameObject explosionPrefab, float deathDelay)
        {
            _eventBus = eventBus;
            _player = player;
            _health = health;
            _sound = sound;
            _explosions = explosions;
            _explosionPrefab = explosionPrefab;
            _deathDelay = deathDelay;
        }

        public void Start()
        {
            _eventBus.Subscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
            _eventBus.Subscribe<GameLoadFinishedEvent>(OnLoadFinished);

            if (_health != null)
            {
                _health.Died += OnHealthDied;
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerDestroyedEvent>(OnPlayerDestroyed);
            _eventBus.Unsubscribe<GameLoadFinishedEvent>(OnLoadFinished);

            if (_health != null)
            {
                _health.Died -= OnHealthDied;
            }
        }

        // Загрузка возвращает корабль в строй: при смерти его только выключили.
        private void OnLoadFinished(GameLoadFinishedEvent _)
        {
            _pending = false;

            if (_player != null)
            {
                _player.gameObject.SetActive(true);
            }
        }

        // Health сам уничтожает объект, поэтому взрыв ставится из этого же кадра.
        private void OnHealthDied() => _eventBus.Publish(new PlayerDestroyedEvent(CombatReason));

        // Нескалированное время: взрыв доигрывает и на паузе диалога.
        public void Tick()
        {
            if (!_pending)
            {
                return;
            }

            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _pending = false;
            _eventBus.Publish(new GameOverEvent(_reason));
        }

        private void OnPlayerDestroyed(PlayerDestroyedEvent evt)
        {
            if (_pending || _player == null)
            {
                return;
            }

            _pending = true;
            _reason = evt.Reason;
            _timer = Mathf.Max(0f, _deathDelay);

            if (_explosionPrefab != null)
            {
                var explosion = UnityEngine.Object.Instantiate(_explosionPrefab,
                    _player.transform.position, Quaternion.identity);
                UnityEngine.Object.Destroy(explosion, _timer + 1f);
            }

            // Свой взрыв — в 2D: он не «где-то там», и затухать с расстоянием ему незачем.
            if (_explosions != null)
            {
                _sound?.Play(_explosions.RandomClip(), _explosions.MixerGroup);
            }

            _player.gameObject.SetActive(false);
        }
    }
}
