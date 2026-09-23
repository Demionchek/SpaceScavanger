using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    // Прячет лётный HUD (квесты, гонка, прицел и т.п.) по правилу HudVisibility:
    // в интерьере, в диалоге и в катсцене. Список корней задаётся в инспекторе.
    // Сам компонент должен висеть на объекте ВНЕ списка, иначе отключит себя.
    public sealed class FlightHudVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject[] _flightHudRoots;

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus, HudVisibility hud)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            SetVisible(hud.IsVisible);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
        }

        private void OnHudVisibilityChanged(HudVisibilityChangedEvent evt) => SetVisible(evt.Visible);

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
