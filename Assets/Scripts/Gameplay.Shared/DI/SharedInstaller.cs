using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Installers/Shared Installer", fileName = "SharedInstaller")]
    public sealed class SharedInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private SaveAssetRegistry _saveRegistry;
        [SerializeField] private StoryDialogueCatalog _storyDialogues;
        [SerializeField] private ResourceCost[] _startingResources;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_saveRegistry);

            builder.Register<ResourceService>(Lifetime.Singleton).As<IResourceService>().As<ISaveable>();
            builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>().As<ISaveable>();
            builder.Register<TradeService>(Lifetime.Singleton).As<ITradeService>();
            builder.Register<QuestService>(Lifetime.Singleton).As<IQuestService>().As<ISaveable>();
            builder.Register<ReputationService>(Lifetime.Singleton).As<IReputationService>().As<ISaveable>();
            builder.Register<ShipStatsService>(Lifetime.Singleton).As<IShipStatsService>().As<ISaveable>();
            builder.Register<RecipeService>(Lifetime.Singleton).As<IRecipeService>().As<ISaveable>();
            builder.Register<CraftingService>(Lifetime.Singleton).As<ICraftingService>();
            builder.Register<UpgradeService>(Lifetime.Singleton).As<IUpgradeService>();
            builder.Register<SoundService>(Lifetime.Singleton).As<ISoundService>();
            builder.Register<JournalService>(Lifetime.Singleton).As<IJournalService>().As<ISaveable>();
            builder.RegisterEntryPoint<JournalRecorder>(Lifetime.Singleton);
            builder.RegisterEntryPoint<StoryDialogueService>(Lifetime.Singleton)
                .WithParameter<StoryDialogueCatalog>(_storyDialogues);
            builder.RegisterEntryPoint<StartingResources>(Lifetime.Singleton)
                .WithParameter<ResourceCost[]>(_startingResources);
            builder.RegisterEntryPoint<SaveService>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<Health>().AsSelf().As<ISaveable>();
        }
    }
}
