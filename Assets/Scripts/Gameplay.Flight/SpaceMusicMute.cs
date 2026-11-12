using System;
using Game.Core;
using UnityEngine;
using UnityEngine.Audio;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // В интерьере играет своя музыка, а космическая оставалась поверх неё — две
    // мелодии разом. Источники ищутся по группе микшера, а не ссылкой из сцены:
    // ассет инсталлера Unity перечитывает сама, а открытую сцену — нет. Глушим,
    // а не останавливаем: вернувшись в полёт, трек продолжается с того же места.
    public sealed class SpaceMusicMute : IInitializable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly AudioMixerGroup _musicGroup;

        public SpaceMusicMute(EventBus eventBus, AudioMixerGroup musicGroup)
        {
            _eventBus = eventBus;
            _musicGroup = musicGroup;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent evt)
        {
            if (_musicGroup == null)
            {
                return;
            }

            var interior = evt.NewState is ShipInteriorState;

            // Ищем заново при каждой смене режима: интерьер — отдельная сцена, и её
            // источники появляются и исчезают вместе с ней.
            var sources = UnityEngine.Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var source in sources)
            {
                if (source.outputAudioMixerGroup == _musicGroup)
                {
                    source.mute = interior;
                }
            }
        }
    }
}
