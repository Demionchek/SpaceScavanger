using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class StoryBeat
    {
        public string Node;
        public StoryDialogueDelivery Delivery;
    }

    [CreateAssetMenu(menuName = "Game/Narrative/Story Dialogue Catalog", fileName = "StoryDialogueCatalog")]
    public sealed class StoryDialogueCatalog : ScriptableObject
    {
        [SerializeField] private StoryBeat[] _beats;

        public int Count => _beats != null ? _beats.Length : 0;

        public StoryBeat GetBeat(int index) =>
            _beats != null && index >= 0 && index < _beats.Length ? _beats[index] : null;
    }
}
