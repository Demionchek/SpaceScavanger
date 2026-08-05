using System;
using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class StoryDialogueService : IStoryDialogueService, IStartable, IDisposable, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly StoryDialogueCatalog _catalog;

        private int _index;
        private string _pending;
        private StoryDialogueDelivery _pendingDelivery;

        public StoryDialogueService(EventBus eventBus, StoryDialogueCatalog catalog)
        {
            _eventBus = eventBus;
            _catalog = catalog;
        }

        public void Start()
        {
            _eventBus.Subscribe<StoryDialogueRequestedEvent>(OnRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<StoryDialogueRequestedEvent>(OnRequested);
        }

        public bool HasPendingFor(StoryDialogueDelivery delivery) =>
            !string.IsNullOrEmpty(_pending) && _pendingDelivery == delivery;

        public void Trigger()
        {
            if (_catalog == null || !string.IsNullOrEmpty(_pending))
            {
                return;
            }

            while (_index < _catalog.Count)
            {
                var beat = _catalog.GetBeat(_index);
                _index++;

                if (beat == null || string.IsNullOrEmpty(beat.Node))
                {
                    continue;
                }

                if (beat.Delivery == StoryDialogueDelivery.Immediate)
                {
                    _eventBus.Publish(new DialogueRequestedEvent(beat.Node));
                    return;
                }

                _pending = beat.Node;
                _pendingDelivery = beat.Delivery;
                _eventBus.Publish(new StoryDialoguePendingEvent(beat.Delivery));
                return;
            }
        }

        public bool TryTakePending(StoryDialogueDelivery delivery, out string node)
        {
            if (!HasPendingFor(delivery))
            {
                node = null;
                return false;
            }

            node = _pending;
            _pending = null;
            return true;
        }

        public string SaveId => "story_dialogue";

        public string Save() => JsonUtility.ToJson(new SaveData
        {
            index = _index,
            pending = _pending,
            delivery = (int)_pendingDelivery
        });

        public void Load(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            _index = data.index;
            _pending = data.pending;
            _pendingDelivery = (StoryDialogueDelivery)data.delivery;

            if (!string.IsNullOrEmpty(_pending))
            {
                _eventBus.Publish(new StoryDialoguePendingEvent(_pendingDelivery));
            }
        }

        private void OnRequested(StoryDialogueRequestedEvent _) => Trigger();

        [Serializable]
        private sealed class SaveData
        {
            public int index;
            public string pending;
            public int delivery;
        }
    }
}
