using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class StoryCall
    {
        public string Node;

        [Tooltip("Кто звонит. Чем лучше отношения, тем чаще он выпадает.")]
        public CharacterDefinition Caller;
    }

    [Serializable]
    public sealed class StoryBeat
    {
        [Tooltip("Нода бита. Игнорируется, если заполнен список вариантов.")]
        public string Node;

        [Tooltip("Варианты звонящего: нода выбирается случайно с весом по отношениям.")]
        public StoryCall[] Variants;

        [Tooltip("Чем бит вызывается. Any берётся любым источником, поэтому прыжок может " +
                 "забрать бит туториала, если игрок улетел, не собрав коридор.")]
        public StoryTrigger Trigger;

        public StoryDialogueDelivery Delivery;

        [Tooltip("Задержка в секундах реального времени между срабатыванием бита и звонком.")]
        public float DelaySeconds;

        [Tooltip("Бит встаёт в очередь вместе с предыдущим, без отдельного триггера — " +
                 "так один прыжок даёт и личный звонок, и звонок корпорации через пару минут.")]
        public bool FollowsPrevious;
    }

    [CreateAssetMenu(menuName = "Game/Narrative/Story Dialogue Catalog", fileName = "StoryDialogueCatalog")]
    public sealed class StoryDialogueCatalog : ScriptableObject
    {
        [SerializeField] private StoryBeat[] _beats;

        [Tooltip("Вес звонящего при нулевых отношениях. Отношения прибавляются к нему, " +
                 "вес не опускается ниже 1 — совсем недоступных звонящих не бывает.")]
        [SerializeField] private int _callerBaseWeight = 10;

        public int Count => _beats != null ? _beats.Length : 0;

        public int CallerBaseWeight => Mathf.Max(1, _callerBaseWeight);

        public StoryBeat GetBeat(int index) =>
            _beats != null && index >= 0 && index < _beats.Length ? _beats[index] : null;
    }
}
