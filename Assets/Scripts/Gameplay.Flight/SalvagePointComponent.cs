using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    // Точка добычи: пока заглушка вместо мини-игры — по F сразу выдаёт добычу.
    // Реальная мини-игра встанет между Interact и выдачей награды.
    public sealed class SalvagePointComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Start extraction";
        [SerializeField] private ResourceType _rewardType = ResourceType.Scrap;
        [SerializeField] private int _rewardAmount = 10;
        [SerializeField] private bool _singleUse = true;
        [SerializeField] private LayerMask _playerMask;
        [SerializeField] private GameObject _promptLabelRoot;

        private bool _used;

        public string Prompt => _prompt;

        private void Awake()
        {
            if (_promptLabelRoot != null)
            {
                _promptLabelRoot.SetActive(false);
            }
        }

        public void Interact(PlayerContext ctx)
        {
            if (_used)
            {
                return;
            }

            _used = _singleUse;

            ctx.ResourceService.Add(_rewardType, _rewardAmount);
            ctx.EventBus.Publish(new SalvageExtractedEvent());

            if (_promptLabelRoot != null)
            {
                _promptLabelRoot.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other) && !_used && _promptLabelRoot != null)
            {
                _promptLabelRoot.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other) && _promptLabelRoot != null)
            {
                _promptLabelRoot.SetActive(false);
            }
        }

        private bool IsPlayer(Collider2D other) => (_playerMask.value & (1 << other.gameObject.layer)) != 0;
    }
}
