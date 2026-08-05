using Game.Core;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class WormholeComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Enter wormhole";
        [SerializeField] private ResourceType _costType;
        [SerializeField] private int _cost = 1;
        [SerializeField] private LayerMask _playerMask;
        [SerializeField] private GameObject _costLabelRoot;
        [SerializeField] private TMP_Text _costLabel;
        [SerializeField] private QuestDefinition _requiredQuest;
        [SerializeField] private string _lockedMessage = "Finish your assignment first";

        private IQuestService _questService;

        [Inject]
        public void Construct(IQuestService questService)
        {
            _questService = questService;
        }

        public string Prompt => _prompt;

        // Без инъекции (например, префаб поставлен в сцену мимо DI) не запираем.
        private bool IsLocked => _requiredQuest != null && _questService != null &&
                                 _questService.GetState(_requiredQuest) != QuestState.TurnedIn;

        private void Awake()
        {
            if (_costLabel != null)
            {
                _costLabel.text = $"Travel: {_cost} {_costType}";
            }

            if (_costLabelRoot != null)
            {
                _costLabelRoot.SetActive(false);
            }
        }

        public void Interact(PlayerContext ctx)
        {
            if (IsLocked)
            {
                ctx.EventBus.Publish(new WarningMessageEvent(_lockedMessage));
                return;
            }

            if (ctx.ResourceService.TrySpend(_costType, _cost))
            {
                ctx.EventBus.Publish(new WormholeTravelRequestedEvent());
            }
            else
            {
                ctx.EventBus.Publish(new NotificationRequestedEvent($"Not enough {_costType}"));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other) && _costLabelRoot != null)
            {
                _costLabelRoot.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other) && _costLabelRoot != null)
            {
                _costLabelRoot.SetActive(false);
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            return (_playerMask.value & (1 << other.gameObject.layer)) != 0;
        }
    }
}
