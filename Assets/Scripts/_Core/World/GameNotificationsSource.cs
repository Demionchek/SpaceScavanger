using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class GameNotificationsSource : IStartable, ITickable, IDisposable
    {
        // Продать десять раз по единице — это десять одинаковых строк подряд, и фид
        // занят ими на полминуты. Поэтому изменения ресурсов копятся и уходят одной
        // строкой на ресурс: ждём, пока поток утихнет, но не дольше потолка.
        private const float CoalesceQuiet = 0.6f;
        private const float CoalesceMaxHold = 2.5f;

        private readonly EventBus _eventBus;
        private readonly Dictionary<ResourceType, int> _pending = new();
        private readonly List<string> _flush = new();
        private float _quiet;
        private float _held;
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

        private void OnLoadStarted(GameLoadStartedEvent _)
        {
            _loading = true;
            _pending.Clear();
        }

        private void OnLoadFinished(GameLoadFinishedEvent _) => _loading = false;

        // Окна торговца и верстака держат паузу, поэтому время тут нескалированное.
        public void Tick()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            _quiet -= Time.unscaledDeltaTime;
            _held += Time.unscaledDeltaTime;

            if (_quiet > 0f && _held < CoalesceMaxHold)
            {
                return;
            }

            _flush.Clear();

            foreach (var pair in _pending)
            {
                if (pair.Value != 0)
                {
                    _flush.Add($"{Signed(pair.Value)} {pair.Key}");
                }
            }

            _pending.Clear();
            _held = 0f;

            foreach (var line in _flush)
            {
                _eventBus.Publish(new NotificationRequestedEvent(line));
            }
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            var delta = evt.NewAmount - _lastResource.GetValueOrDefault(evt.Type);
            _lastResource[evt.Type] = evt.NewAmount;

            if (delta == 0 || _loading)
            {
                return;
            }

            _pending[evt.Type] = _pending.GetValueOrDefault(evt.Type) + delta;
            _quiet = CoalesceQuiet;
        }

        private void OnReputationChanged(ReputationChangedEvent evt)
        {
            var delta = evt.NewValue - _lastReputation.GetValueOrDefault(evt.Group);
            _lastReputation[evt.Group] = evt.NewValue;

            if (delta != 0 && !_loading)
            {
                _eventBus.Publish(new NotificationRequestedEvent(
                    $"{Name(evt.Group)} reputation {Signed(delta)}"));
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

        // Без этого в ленту уезжал ToString() ассета — "Scavangers_Group (Game.Core.NpcGroup)".
        private static string Name(NpcGroup group) =>
            group == null ? "Unknown" :
            string.IsNullOrEmpty(group.DisplayName) ? group.name : group.DisplayName;

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
    }
}
