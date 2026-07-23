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
            builder.RegisterComponentInHierarchy<RaceHudUI>();
            builder.RegisterComponentInHierarchy<ModeTransitionController>();

            // Опционально: если в сцене есть скрыватель лётного HUD — подключаем.
            var hudVisibility = Object.FindFirstObjectByType<FlightHudVisibility>(FindObjectsInactive.Include);
            if (hudVisibility != null)
            {
                builder.RegisterComponent(hudVisibility);
            }

            // Опционально: радар (собирается в сцене отдельно).
            var minimap = Object.FindFirstObjectByType<MinimapUI>(FindObjectsInactive.Include);
            if (minimap != null)
            {
                builder.RegisterComponent(minimap);
            }
        }
    }
}
