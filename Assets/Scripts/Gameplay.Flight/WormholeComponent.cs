using Game.Core;
using TMPro;
using UnityEngine;

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

        public string Prompt => _prompt;

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
