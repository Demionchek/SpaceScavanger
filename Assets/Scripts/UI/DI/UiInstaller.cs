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

            var boundary = Object.FindFirstObjectByType<BoundaryDeathController>(FindObjectsInactive.Include);
            if (boundary != null)
            {
                builder.RegisterComponent(boundary);
            }

            var gameOver = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            if (gameOver != null)
            {
                builder.RegisterComponent(gameOver);
            }

            var notifications = Object.FindFirstObjectByType<NotificationFeedUI>(FindObjectsInactive.Include);
            if (notifications != null)
            {
                builder.RegisterComponent(notifications);
            }

            var journal = Object.FindFirstObjectByType<JournalUI>(FindObjectsInactive.Include);
            if (journal != null)
            {
                builder.RegisterComponent(journal);
            }

            var inventory = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
            if (inventory != null)
            {
                builder.RegisterComponent(inventory);
            }

            var tutorial = Object.FindFirstObjectByType<TutorialPopupUI>(FindObjectsInactive.Include);
            if (tutorial != null)
            {
                builder.RegisterComponent(tutorial);
            }

            var warningBanner = Object.FindFirstObjectByType<WarningBannerUI>(FindObjectsInactive.Include);
            if (warningBanner != null)
            {
                builder.RegisterComponent(warningBanner);
            }
        }
    }
}
