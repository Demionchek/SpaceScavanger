using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Narrative/Random Event Catalog", fileName = "RandomEventCatalog")]
    public sealed class RandomEventCatalog : ScriptableObject
    {
        [SerializeField] private string[] _eventNodes;

        public bool HasEvents => _eventNodes != null && _eventNodes.Length > 0;

        public string GetRandomNode()
        {
            return _eventNodes[Random.Range(0, _eventNodes.Length)];
        }
    }
}
