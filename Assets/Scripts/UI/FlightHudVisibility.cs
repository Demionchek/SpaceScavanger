using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    // Прячет лётный HUD (квесты, гонка, прицел и т.п.) внутри корабля и
    // показывает в полёте. Список корней задаётся в инспекторе. Сам компонент
    // должен висеть на объекте ВНЕ списка, иначе отключит себя (подписка,
    // впрочем, переживёт — но так чище).
    public sealed class FlightHudVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject[] _flightHudRoots;

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;

            // Подписка в Construct, чтобы поймать стартовое событие SpaceFlightState.
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState is ShipInteriorState)
            {
                SetVisible(false);
            }
            else if (evt.NewState is SpaceFlightState)
            {
                SetVisible(true);
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (var root in _flightHudRoots)
            {
                if (root != null)
                {
                    root.SetActive(visible);
                }
            }
        }
    }
}
