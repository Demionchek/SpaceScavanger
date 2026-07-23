using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class PlayerMarker : MonoBehaviour, IPlayerLocator
    {
        public Vector2 Position => transform.position;
    }
}
