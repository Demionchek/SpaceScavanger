using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    // Корпоративная квота: сдать ресурс, пока не кончились отведённые секторы.
    public sealed class QuotaGoal : IQuestGoal, IDeliverableGoal, ISaveableGoal, IQuestGoalStatus
    {
        private readonly QuestDefinition _quest;
        private readonly ResourceType _resource;
        private readonly int _baseAmount;
        private readonly int _amountPerSector;
        private readonly int _deadlineSectors;
        private readonly IResourceService _resources;
        private readonly ISectorService _sectors;

        private EventBus _bus;
        private int _issuedSector;
        private bool _completionAnnounced;

        public QuotaGoal(QuestDefinition quest, ResourceType resource, int baseAmount, int amountPerSector,
            int deadlineSectors, IResourceService resources, ISectorService sectors)
        {
            _quest = quest;
            _resource = resource;
            _baseAmount = baseAmount;
            _amountPerSector = amountPerSector;
            _deadlineSectors = deadlineSectors;
            _resources = resources;
            _sectors = sectors;
            _issuedSector = sectors.Current;
        }

        // Квота тяжелеет с глубиной сектора, в котором её выдали. Номер этого сектора
        // и так лежит в сейве цели, поэтому само число запоминать не нужно.
        public int Amount => Mathf.Max(1, _baseAmount + _amountPerSector * (_issuedSector - 1));

        public bool IsComplete => _resources.GetAmount(_resource) >= Amount;

        public float Progress => Mathf.Clamp01((float)_resources.GetAmount(_resource) / Amount);

        // Сектор выдачи входит в срок: выдана во втором, дедлайн 3 — сдать во 2, 3 или 4.
        public int SectorsLeft => _issuedSector + _deadlineSectors - _sectors.Current;

        public bool IsOverdue => SectorsLeft <= 0;

        public string Status
        {
            get
            {
                var carried = Mathf.Min(_resources.GetAmount(_resource), Amount);
                var deadline = IsOverdue ? "overdue" : $"{SectorsLeft} sectors left";
                return $"{carried}/{Amount} {_resource} — {deadline}";
            }
        }

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            _bus = null;
        }

        public bool TryDeliver()
        {
            return _resources.TrySpend(_resource, Amount);
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (evt.Type != _resource)
            {
                return;
            }

            if (IsComplete && !_completionAnnounced)
            {
                _completionAnnounced = true;
                _bus.Publish(new QuestCompletedEvent(_quest));
            }
            else if (!IsComplete)
            {
                _completionAnnounced = false;
            }
        }

        public string SaveProgress()
        {
            return JsonUtility.ToJson(new SaveData { issuedSector = _issuedSector });
        }

        public void LoadProgress(string data)
        {
            var saved = JsonUtility.FromJson<SaveData>(data);
            if (saved != null && saved.issuedSector > 0)
            {
                _issuedSector = saved.issuedSector;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int issuedSector;
        }
    }
}
