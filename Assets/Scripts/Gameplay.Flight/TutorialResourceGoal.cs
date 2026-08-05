using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Все ресурсы под _resourcesRoot собраны -> следующий сюжетный диалог.
    public sealed class TutorialResourceGoal : MonoBehaviour
    {
        [SerializeField] private Transform _resourcesRoot;

        private readonly List<HookableResource> _tracked = new();
        private EventBus _eventBus;
        private bool _done;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Start()
        {
            var root = _resourcesRoot != null ? _resourcesRoot : transform;
            root.GetComponentsInChildren(true, _tracked);
            _done = _tracked.Count == 0;
        }

        private void Update()
        {
            if (_done)
            {
                return;
            }

            foreach (var resource in _tracked)
            {
                if (resource != null)
                {
                    return;
                }
            }

            _done = true;
            _eventBus.Publish(new StoryDialogueRequestedEvent());
        }
    }
}
