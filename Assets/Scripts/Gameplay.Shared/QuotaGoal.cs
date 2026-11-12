using System;
using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    // Корпоративная квота: сдать груз, пока не кончились отведённые секторы. Чем
    // глубже сектор выдачи, тем больше строк в накладной.
    public sealed class QuotaGoal : IQuestGoal, IDeliverableGoal, ISaveableGoal, IQuestGoalStatus
    {
        private readonly QuestDefinition _quest;
        private readonly QuotaDemand[] _demands;
        private readonly int _deadlineSectors;
        private readonly IResourceService _resources;
        private readonly ISectorService _sectors;
        private readonly List<Line> _lines = new();

        private EventBus _bus;
        private int _issuedSector;
        private bool _linesReady;
        private bool _completionAnnounced;

        public QuotaGoal(QuestDefinition quest, QuotaDemand[] demands, int deadlineSectors,
            IResourceService resources, ISectorService sectors)
        {
            _quest = quest;
            _demands = demands;
            _deadlineSectors = deadlineSectors;
            _resources = resources;
            _sectors = sectors;
            _issuedSector = sectors.Current;
        }

        public bool IsComplete
        {
            get
            {
                foreach (var line in Lines)
                {
                    if (_resources.GetAmount(line.Resource) < line.Amount)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public float Progress
        {
            get
            {
                var carried = 0f;
                var needed = 0f;

                foreach (var line in Lines)
                {
                    carried += Mathf.Min(_resources.GetAmount(line.Resource), line.Amount);
                    needed += line.Amount;
                }

                return needed > 0f ? Mathf.Clamp01(carried / needed) : 1f;
            }
        }

        // Сектор выдачи входит в срок: выдана во втором, дедлайн 3 — сдать во 2, 3 или 4.
        public int SectorsLeft => _issuedSector + _deadlineSectors - _sectors.Current;

        public bool IsOverdue => SectorsLeft <= 0;

        public string Status
        {
            get
            {
                var text = new StringBuilder();

                foreach (var line in Lines)
                {
                    if (text.Length > 0)
                    {
                        text.Append(", ");
                    }

                    text.Append(line.Resource)
                        .Append(' ')
                        .Append(Mathf.Min(_resources.GetAmount(line.Resource), line.Amount))
                        .Append('/')
                        .Append(line.Amount);
                }

                text.Append(" — ").Append(IsOverdue ? "overdue" : $"{SectorsLeft} sectors left");
                return text.ToString();
            }
        }

        // Накладная словами: её читает офицер по интеркому, поэтому текст строится
        // здесь, а не в диалоге — там числа пришлось бы держать руками.
        public string DemandText
        {
            get
            {
                var text = new StringBuilder();

                for (var i = 0; i < Lines.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Append(i == Lines.Count - 1 ? " and " : ", ");
                    }

                    text.Append(Lines[i].Amount)
                        .Append(i == 0 ? " units of " : " of ")
                        .Append(Lines[i].Resource.ToString().ToLowerInvariant());
                }

                return text.ToString();
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
            if (!IsComplete)
            {
                return false;
            }

            foreach (var line in Lines)
            {
                _resources.TrySpend(line.Resource, line.Amount);
            }

            return true;
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (!Demanded(evt.Type))
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

        private bool Demanded(ResourceType type)
        {
            foreach (var line in Lines)
            {
                if (line.Resource == type)
                {
                    return true;
                }
            }

            return false;
        }

        // Строки считаются один раз, но только после загрузки: сектор выдачи
        // приходит из сейва уже после конструктора.
        private List<Line> Lines
        {
            get
            {
                if (_linesReady)
                {
                    return _lines;
                }

                _linesReady = true;
                _lines.Clear();

                if (_demands != null)
                {
                    foreach (var demand in _demands)
                    {
                        if (demand != null && demand.AppliesTo(_issuedSector))
                        {
                            _lines.Add(new Line(demand.Resource, demand.AmountFor(_issuedSector)));
                        }
                    }
                }

                return _lines;
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
                _linesReady = false;
            }
        }

        private readonly struct Line
        {
            public readonly ResourceType Resource;
            public readonly int Amount;

            public Line(ResourceType resource, int amount)
            {
                Resource = resource;
                Amount = amount;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int issuedSector;
        }
    }
}
