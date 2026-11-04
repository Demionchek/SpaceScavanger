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
        private readonly int _amount;
        private readonly int _deadlineSectors;
        private readonly IResourceService _resources;
        private readonly ISectorService _sectors;

        private EventBus _bus;
        private int _issuedSector;
        private bool _completionAnnounced;

        public QuotaGoal(QuestDefinition quest, ResourceType resource, int amount, int deadlineSectors,
            IResourceService resources, ISectorService sectors)
        {
            _quest = quest;
            _resource = resource;
            _amount = amount;
            _deadlineSectors = deadlineSectors;
            _resources = resources;
            _sectors = sectors;
            _issuedSector = sectors.Current;
        }

        public bool IsComplete => _resources.GetAmount(_resource) >= _amount;

        public float Progress => Mathf.Clamp01((float)_resources.GetAmount(_resource) / _amount);

        // Сектор выдачи входит в срок: выдана во втором, дедлайн 3 — сдать во 2, 3 или 4.
        public int SectorsLeft => _issuedSector + _deadlineSectors - _sectors.Current;

        public bool IsOverdue => SectorsLeft <= 0;

        public string Status
        {
            get
            {
                var carried = Mathf.Min(_resources.GetAmount(_resource), _amount);
                var deadline = IsOverdue ? "overdue" : $"{SectorsLeft} sectors left";
                return $"{carried}/{_amount} {_resource} — {deadline}";
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
            return _resources.TrySpend(_resource, _amount);
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
