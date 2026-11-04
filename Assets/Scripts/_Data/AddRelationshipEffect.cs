using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Add Relationship", fileName = "AddRelationshipEffect")]
    public sealed class AddRelationshipEffect : ChoiceEffect
    {
        [SerializeField] private CharacterDefinition _character;
        [SerializeField] private int _amount = 1;

        public override void Apply(GameContext ctx)
        {
            ctx.RelationshipService.Add(_character, _amount);
        }
    }
}
