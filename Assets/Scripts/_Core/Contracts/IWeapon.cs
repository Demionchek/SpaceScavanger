using UnityEngine;

namespace Game.Core
{
    public interface IWeapon
    {
        void Fire(Vector2 origin, Vector2 direction, GameObject source);
    }
}
