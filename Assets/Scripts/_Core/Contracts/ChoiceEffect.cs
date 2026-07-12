using UnityEngine;

namespace Game.Core
{
    public abstract class ChoiceEffect : ScriptableObject
    {
        public abstract void Apply(GameContext ctx);
    }
}
