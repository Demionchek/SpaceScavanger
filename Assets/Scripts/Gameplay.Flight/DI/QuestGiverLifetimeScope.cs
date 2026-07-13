using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public class QuestGiverLifetimeScope : LifetimeScope
    {
        [SerializeField] private QuestGiverComponent questGiverComponent;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(questGiverComponent);
        }
    }
}
