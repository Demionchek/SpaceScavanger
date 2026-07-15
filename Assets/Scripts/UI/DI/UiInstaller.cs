using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    [CreateAssetMenu(menuName = "Game/Installers/UI Installer", fileName = "UiInstaller")]
    public sealed class UiInstaller : ScriptableObjectInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<HealthBarUI>();
            builder.RegisterComponentInHierarchy<TraderUI>();
            builder.RegisterComponentInHierarchy<QuestHudUI>();
            builder.RegisterComponentInHierarchy<WorkbenchUI>();
            builder.RegisterComponentInHierarchy<ShipInfoUI>();
        }
    }
}
