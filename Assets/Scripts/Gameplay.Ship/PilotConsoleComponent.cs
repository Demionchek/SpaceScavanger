using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Ship
{
    // Кресло/консоль пилота в интерьере: взаимодействие возвращает в полёт
    // (тот же ModeSwitchRequestedEvent, что и клавиша Dock).
    public sealed class PilotConsoleComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Pilot seat";

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            _eventBus.Publish(new ModeSwitchRequestedEvent());
        }
    }
}
