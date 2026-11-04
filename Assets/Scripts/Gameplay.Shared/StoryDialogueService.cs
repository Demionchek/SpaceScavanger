using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class StoryDialogueService : IStoryDialogueService, IStartable, ITickable, IDisposable, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly StoryDialogueCatalog _catalog;
        private readonly IRelationshipService _relationships;
        private readonly List<Call> _scheduled = new();
        private readonly List<Call> _pending = new();

        private int _index;
        private bool _dialogueActive;
        private bool _cutsceneActive;

        public StoryDialogueService(EventBus eventBus, StoryDialogueCatalog catalog,
            IRelationshipService relationships)
        {
            _eventBus = eventBus;
            _catalog = catalog;
            _relationships = relationships;
        }

        public void Start()
        {
            _eventBus.Subscribe<StoryDialogueRequestedEvent>(OnRequested);
            _eventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Subscribe<CutsceneStartedEvent>(OnCutsceneStarted);
            _eventBus.Subscribe<CutsceneFinishedEvent>(OnCutsceneFinished);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<StoryDialogueRequestedEvent>(OnRequested);
            _eventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Unsubscribe<CutsceneStartedEvent>(OnCutsceneStarted);
            _eventBus.Unsubscribe<CutsceneFinishedEvent>(OnCutsceneFinished);
        }

        public bool HasPendingFor(StoryDialogueDelivery delivery)
        {
            foreach (var call in _pending)
            {
                if (call.Delivery == delivery)
                {
                    return true;
                }
            }

            return false;
        }

        // Пока предыдущий звонок не отвечен, сюжет не двигаем — иначе бит потерялся бы.
        public void Trigger(StoryTrigger source)
        {
            if (_catalog == null || _scheduled.Count > 0 || _pending.Count > 0)
            {
                return;
            }

            var target = FindBeat(source);
            if (target < 0)
            {
                return;
            }

            // Пропущенные биты (игрок улетел, не закончив туториал) забираются вместе
            // с найденным — иначе они съели бы следующий триггер.
            while (_index <= target)
            {
                Schedule(_catalog.GetBeat(_index));
                _index++;
            }

            while (NextFollowsPrevious())
            {
                Schedule(_catalog.GetBeat(_index));
                _index++;
            }

            ReleaseDue();
        }

        // Индекс сюжета не трогаем: такой звонок встаёт в ту же очередь, но
        // своей очереди бит не занимает.
        public void Enqueue(string node, StoryDialogueDelivery delivery, float delaySeconds = 0f)
        {
            if (string.IsNullOrEmpty(node))
            {
                return;
            }

            _scheduled.Add(new Call
            {
                Node = node,
                Delivery = delivery,
                Delay = Mathf.Max(0f, delaySeconds)
            });

            ReleaseDue();
        }

        public bool TryTakePending(StoryDialogueDelivery delivery, out string node)
        {
            for (var i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].Delivery != delivery)
                {
                    continue;
                }

                node = _pending[i].Node;
                _pending.RemoveAt(i);
                return true;
            }

            node = null;
            return false;
        }

        // Отложенные звонки идут по реальному времени: пауза диалога и катсцена их не морозят.
        public void Tick()
        {
            if (_scheduled.Count == 0)
            {
                return;
            }

            var delta = Time.unscaledDeltaTime;
            foreach (var call in _scheduled)
            {
                call.Delay -= delta;
            }

            ReleaseDue();
        }

        // Бит с FollowsPrevious едет прицепом и сам триггером не выбирается.
        private int FindBeat(StoryTrigger source)
        {
            for (var i = _index; i < _catalog.Count; i++)
            {
                var beat = _catalog.GetBeat(i);
                if (beat == null || beat.FollowsPrevious)
                {
                    continue;
                }

                if (beat.Trigger == StoryTrigger.Any || beat.Trigger == source)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Schedule(StoryBeat beat)
        {
            var node = PickNode(beat);
            if (string.IsNullOrEmpty(node))
            {
                return;
            }

            _scheduled.Add(new Call
            {
                Node = node,
                Delivery = beat.Delivery,
                Delay = Mathf.Max(0f, beat.DelaySeconds)
            });
        }

        private bool NextFollowsPrevious()
        {
            var beat = _catalog.GetBeat(_index);
            return beat != null && beat.FollowsPrevious;
        }

        // Звонящий выбирается случайно, но чем лучше отношения, тем чаще он звонит.
        private string PickNode(StoryBeat beat)
        {
            if (beat == null)
            {
                return null;
            }

            if (beat.Variants == null || beat.Variants.Length == 0)
            {
                return beat.Node;
            }

            var total = 0;
            foreach (var variant in beat.Variants)
            {
                total += WeightOf(variant);
            }

            if (total <= 0)
            {
                return beat.Node;
            }

            var roll = UnityEngine.Random.Range(0, total);
            foreach (var variant in beat.Variants)
            {
                roll -= WeightOf(variant);
                if (roll < 0)
                {
                    return variant.Node;
                }
            }

            return beat.Node;
        }

        private int WeightOf(StoryCall variant)
        {
            if (variant == null || string.IsNullOrEmpty(variant.Node))
            {
                return 0;
            }

            var attitude = variant.Caller != null ? _relationships.Get(variant.Caller) : 0;
            return Mathf.Max(1, _catalog.CallerBaseWeight + attitude);
        }

        private void ReleaseDue()
        {
            for (var i = 0; i < _scheduled.Count; i++)
            {
                var call = _scheduled[i];
                if (call.Delay > 0f)
                {
                    continue;
                }

                // Immediate играется сразу, поэтому ждёт окна без диалога и катсцены.
                if (call.Delivery == StoryDialogueDelivery.Immediate)
                {
                    if (_dialogueActive || _cutsceneActive)
                    {
                        continue;
                    }

                    _scheduled.RemoveAt(i--);
                    _eventBus.Publish(new DialogueRequestedEvent(call.Node));
                    continue;
                }

                _scheduled.RemoveAt(i--);
                _pending.Add(call);
                _eventBus.Publish(new StoryDialoguePendingEvent(call.Delivery));
            }
        }

        private void OnRequested(StoryDialogueRequestedEvent evt) => Trigger(evt.Source);

        private void OnDialogueStarted(DialogueStartedEvent _) => _dialogueActive = true;

        private void OnDialogueFinished(DialogueFinishedEvent _) => _dialogueActive = false;

        private void OnCutsceneStarted(CutsceneStartedEvent _) => _cutsceneActive = true;

        private void OnCutsceneFinished(CutsceneFinishedEvent _) => _cutsceneActive = false;

        public string SaveId => "story_dialogue";

        public string Save()
        {
            var data = new SaveData { index = _index };

            foreach (var call in _scheduled)
            {
                data.scheduledNodes.Add(call.Node);
                data.scheduledDeliveries.Add((int)call.Delivery);
                data.scheduledDelays.Add(call.Delay);
            }

            foreach (var call in _pending)
            {
                data.pendingNodes.Add(call.Node);
                data.pendingDeliveries.Add((int)call.Delivery);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _scheduled.Clear();
            _pending.Clear();

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            _index = data.index;

            for (var i = 0; i < data.scheduledNodes.Count; i++)
            {
                _scheduled.Add(new Call
                {
                    Node = data.scheduledNodes[i],
                    Delivery = (StoryDialogueDelivery)data.scheduledDeliveries[i],
                    Delay = data.scheduledDelays[i]
                });
            }

            for (var i = 0; i < data.pendingNodes.Count; i++)
            {
                var call = new Call
                {
                    Node = data.pendingNodes[i],
                    Delivery = (StoryDialogueDelivery)data.pendingDeliveries[i]
                };

                _pending.Add(call);
                _eventBus.Publish(new StoryDialoguePendingEvent(call.Delivery));
            }
        }

        private sealed class Call
        {
            public string Node;
            public StoryDialogueDelivery Delivery;
            public float Delay;
        }

        [Serializable]
        private sealed class SaveData
        {
            public int index;
            public List<string> scheduledNodes = new();
            public List<int> scheduledDeliveries = new();
            public List<float> scheduledDelays = new();
            public List<string> pendingNodes = new();
            public List<int> pendingDeliveries = new();
        }
    }
}
