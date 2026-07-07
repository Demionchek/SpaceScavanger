using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public class TraderLifetimeScope : LifetimeScope
    {
        [SerializeField] private TraderComponent _trader;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_trader);
        }
    }
}
