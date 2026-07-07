using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Installers/Shared Installer", fileName = "SharedInstaller")]
    public sealed class SharedInstaller : ScriptableObjectInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<ResourceService>(Lifetime.Singleton).As<IResourceService>();
            builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>();
            builder.Register<TradeService>(Lifetime.Singleton).As<ITradeService>();
            builder.Register<SoundService>(Lifetime.Singleton).As<ISoundService>();
            builder.RegisterComponentInHierarchy<Health>();
        }
    }
}
