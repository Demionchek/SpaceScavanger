using UnityEngine;

namespace Game.Core
{
    public readonly struct EnemyAlertEvent
    {
        public readonly Vector2 Position;
        public readonly float Radius;
        public readonly float Delay;

        public EnemyAlertEvent(Vector2 position, float radius, float delay)
        {
            Position = position;
            Radius = radius;
            Delay = delay;
        }
    }
}
