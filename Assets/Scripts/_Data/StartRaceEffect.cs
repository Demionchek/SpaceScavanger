using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Start Race", fileName = "StartRaceEffect")]
    public sealed class StartRaceEffect : ChoiceEffect
    {
        [SerializeField] private RaceDefinition _race;

        public override void Apply(GameContext ctx)
        {
            ctx.EventBus.Publish(new RaceRequestedEvent(_race));
        }
    }
}
