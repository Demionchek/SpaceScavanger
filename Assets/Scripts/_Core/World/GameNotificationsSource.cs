using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class GameNotificationsSource : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly Dictionary<ResourceType, int> _lastResource = new();
        private readonly Dictionary<NpcGroup, int> _lastReputation = new();
        private readonly Dictionary<CharacterDefinition, int> _lastRelationship = new();
        private bool _loading;

        public GameNotificationsSource(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Subscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnStoryDialoguePending);
            _eventBus.Subscribe<SectorChangedEvent>(OnSectorChanged);
            _eventBus.Subscribe<GameLoadStartedEvent>(OnLoadStarted);
            _eventBus.Subscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Unsubscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Unsubscribe<StoryDialoguePendingEvent>(OnStoryDialoguePending);
            _eventBus.Unsubscribe<SectorChangedEvent>(OnSectorChanged);
            _eventBus.Unsubscribe<GameLoadStartedEvent>(OnLoadStarted);
            _eventBus.Unsubscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        private void OnLoadStarted(GameLoadStartedEvent _) => _loading = true;
        private void OnLoadFinished(GameLoadFinishedEvent _) => _loading = false;

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            var delta = evt.NewAmount - _lastResource.GetValueOrDefault(evt.Type);
            _lastResource[evt.Type] = evt.NewAmount;

            if (delta != 0 && !_loading)
            {
                _eventBus.Publish(new NotificationRequestedEvent($"{Signed(delta)} {evt.Type}"));
            }
        }

        private void OnReputationChanged(ReputationChangedEvent evt)
        {
            var delta = evt.NewValue - _lastReputation.GetValueOrDefault(evt.Group);
            _lastReputation[evt.Group] = evt.NewValue;

            if (delta != 0 && !_loading)
            {
                _eventBus.Publish(new NotificationRequestedEvent($"{evt.Group} reputation {Signed(delta)}"));
            }
        }

        private void OnRelationshipChanged(RelationshipChangedEvent evt)
        {
            var delta = evt.NewValue - _lastRelationship.GetValueOrDefault(evt.Character);
            _lastRelationship[evt.Character] = evt.NewValue;

            if (delta != 0 && !_loading)
            {
                _eventBus.Publish(new NotificationRequestedEvent(
                    $"{evt.Character.DisplayName} attitude {Signed(delta)}"));
            }
        }

        // Набрал нужное — узнать об этом надо сразу: в фиде строка встанет в очередь
        // за «+1 Scrap», поэтому она же идёт баннером по центру экрана.
        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (_loading || evt.Quest == null)
            {
                return;
            }

            var message = $"Objective complete: {evt.Quest.Title}";
            _eventBus.Publish(new NotificationRequestedEvent(message));
            _eventBus.Publish(new WarningMessageEvent(message));
        }

        // Звонок может прийти, когда игрок в полёте, а интерком — в ещё не загруженном интерьере.
        private void OnStoryDialoguePending(StoryDialoguePendingEvent evt)
        {
            if (_loading)
            {
                return;
            }

            var message = evt.Delivery switch
            {
                StoryDialogueDelivery.Intercom => "Incoming call on the ship intercom",
                StoryDialogueDelivery.ShipComputer => "New message on the ship computer",
                _ => null
            };

            if (message != null)
            {
                _eventBus.Publish(new NotificationRequestedEvent(message));
            }
        }

        private void OnSectorChanged(SectorChangedEvent evt)
        {
            if (!_loading)
            {
                _eventBus.Publish(new NotificationRequestedEvent($"Sector {evt.Number}"));
            }
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
    }
}
