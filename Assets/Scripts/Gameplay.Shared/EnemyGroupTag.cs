using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Shared
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyGroupTag : MonoBehaviour
    {
        [SerializeField] private string _groupTag;

        private EventBus _eventBus;

        public void SetGroupTag(string groupTag)
        {
            _groupTag = groupTag;
        }

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Awake()
        {
            GetComponent<Health>().Died += OnDied;
        }

        private void OnDied()
        {
            if (_eventBus != null && !string.IsNullOrEmpty(_groupTag))
            {
                _eventBus.Publish(new EnemyDestroyedEvent(_groupTag));
            }
        }
    }
}
