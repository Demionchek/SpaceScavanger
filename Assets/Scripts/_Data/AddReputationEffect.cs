using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Add Reputation", fileName = "AddReputationEffect")]
    public sealed class AddReputationEffect : ChoiceEffect
    {
        [SerializeField] private NpcGroup _group;
        [SerializeField] private int _amount = 1;

        public override void Apply(GameContext ctx)
        {
            ctx.ReputationService.Add(_group, _amount);
        }
    }
}
