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

        [Tooltip("Квест корпоративной квоты: по нему идёт звонок-приёмка и проверка срока.")]
        [SerializeField] private QuestDefinition _quotaQuest;

        [Tooltip("Объявление о следующей квоте: висит в панели заданий от сдачи до прыжка.")]
        [SerializeField] private QuestDefinition _nextQuotaNotice;

        [Header("Починка корпуса")]
        [SerializeField] private ResourceType _repairResource = ResourceType.Scrap;

        [Tooltip("Сколько единиц корпуса восстанавливает одна единица ресурса.")]
        [SerializeField] private int _hullPerRepairUnit = 5;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_saveRegistry);

            builder.Register<ResourceService>(Lifetime.Singleton).As<IResourceService>().As<ISaveable>();
            builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>().As<ISaveable>();
            builder.Register<TradeService>(Lifetime.Singleton).As<ITradeService>();
            builder.Register<QuestService>(Lifetime.Singleton).As<IQuestService>().As<ISaveable>();
            builder.Register<ReputationService>(Lifetime.Singleton).As<IReputationService>().As<ISaveable>();
            builder.Register<RelationshipService>(Lifetime.Singleton).As<IRelationshipService>().As<ISaveable>();
            builder.Register<ShipStatsService>(Lifetime.Singleton).As<IShipStatsService>().As<ISaveable>();
            builder.Register<RecipeService>(Lifetime.Singleton).As<IRecipeService>().As<ISaveable>();
            builder.Register<CraftingService>(Lifetime.Singleton).As<ICraftingService>();
            builder.Register<UpgradeService>(Lifetime.Singleton).As<IUpgradeService>().As<ISaveable>();
            builder.Register<SoundService>(Lifetime.Singleton).As<ISoundService>();
            builder.Register<RepairService>(Lifetime.Singleton).As<IRepairService>()
                   .WithParameter("resource", _repairResource)
                   .WithParameter("hullPerUnit", _hullPerRepairUnit);
            builder.Register<JournalService>(Lifetime.Singleton).As<IJournalService>().As<ISaveable>();
            builder.Register<InteractionHintService>(Lifetime.Singleton).As<IInteractionHintService>().As<ISaveable>();
            builder.RegisterEntryPoint<SectorService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<QuotaService>(Lifetime.Singleton)
                .WithParameter("quotaQuest", _quotaQuest)
                .WithParameter("waitingQuest", _nextQuotaNotice);
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
