using System;
using Game.Core;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class SectorService : ISectorService, IStartable, IDisposable, ISaveable
    {
        private readonly EventBus _eventBus;

        private int _current = 1;

        public SectorService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public int Current => _current;

        public void Start()
        {
            _eventBus.Subscribe<WormholeTravelRequestedEvent>(OnTravelRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<WormholeTravelRequestedEvent>(OnTravelRequested);
        }

        public void Advance()
        {
            _current++;
            _eventBus.Publish(new SectorChangedEvent(_current));
        }

        // Прыжок оплачен ещё в WormholeComponent, так что отсюда он уже не сорвётся.
        private void OnTravelRequested(WormholeTravelRequestedEvent _) => Advance();

        public string SaveId => "sector";

        public string Save()
        {
            return JsonUtility.ToJson(new SaveData { current = _current });
        }

        public void Load(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _current = data != null && data.current > 0 ? data.current : 1;
            _eventBus.Publish(new SectorChangedEvent(_current));
        }

        [Serializable]
        private sealed class SaveData
        {
            public int current;
        }
    }
}
