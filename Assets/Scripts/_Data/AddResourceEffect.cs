using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Add Resource", fileName = "AddResourceEffect")]
    public sealed class AddResourceEffect : ChoiceEffect
    {
        [SerializeField] private ResourceType _resourceType;
        [SerializeField] private int _amount = 1;

        public override void Apply(GameContext ctx)
        {
            ctx.ResourceService.Add(_resourceType, _amount);
        }
    }
}
