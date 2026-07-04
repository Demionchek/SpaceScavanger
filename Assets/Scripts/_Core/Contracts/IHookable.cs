using UnityEngine;

namespace Game.Core
{
    public interface IHookable
    {
        int RequiredHookLevel { get; }
        Vector2 Position { get; }
        void OnGrabbed(HookContext ctx);
    }
}
