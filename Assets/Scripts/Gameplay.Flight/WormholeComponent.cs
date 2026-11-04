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

        [TextArea]
        [SerializeField] private string _confirmMessage =
            "Jumping through the wormhole is one-way. You will not be able to return to this sector. Proceed?";

        [SerializeField] private string _confirmLabel = "Jump";
        [SerializeField] private string _cancelLabel = "Stay";

        private IQuestService _questService;
        private IQuotaService _quotaService;

        [Inject]
        public void Construct(IQuestService questService, IQuotaService quotaService)
        {
            _questService = questService;
            _quotaService = quotaService;
        }

        public string Prompt => _prompt;

        // Без инъекции (например, префаб поставлен в сцену мимо DI) не запираем.
        private bool IsLocked => _requiredQuest != null && _questService != null &&
                                 _questService.GetState(_requiredQuest) != QuestState.TurnedIn;

        private void Awake()
        {
            RefreshCostLabel();

            if (_costLabelRoot != null)
            {
                _costLabelRoot.SetActive(false);
            }
        }

        // Цену ставит ZoneSpawner по тиру сектора: чем дальше, тем дороже прыжок.
        public void SetCost(int cost)
        {
            _cost = cost;
            RefreshCostLabel();
        }

        private void RefreshCostLabel()
        {
            if (_costLabel != null)
            {
                _costLabel.text = $"Travel: {_cost} {_costType}";
            }
        }

        public void Interact(PlayerContext ctx)
        {
            if (IsLocked)
            {
                ctx.EventBus.Publish(new WarningMessageEvent(_lockedMessage));
                return;
            }

            // Ресурс списываем только после подтверждения — отказ не должен стоить топлива.
            ctx.EventBus.Publish(new ConfirmRequestedEvent(_confirmMessage, () => Travel(ctx), _confirmLabel,
                _cancelLabel));
        }

        private void Travel(PlayerContext ctx)
        {
            // Срок квоты проверяется до перехода: корпорация подрывает корабль здесь,
            // иначе взрыв пришёлся бы на побелевший экран перехода.
            if (_quotaService != null && _quotaService.TryEnforceDeadline())
            {
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
