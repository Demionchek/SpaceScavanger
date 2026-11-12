using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    [CreateAssetMenu(menuName = "Game/Installers/UI Installer", fileName = "UiInstaller")]
    public sealed class UiInstaller : ScriptableObjectInstaller
    {
        [Header("Вид окон, которые собираются кодом")]
        [Tooltip("Рама окна: тот же спрайт, что у инвентаря и JumpConfirm.")]
        [SerializeField] private Sprite _windowFrame;

        [Tooltip("Внутреннее поле: подложка под полосу и под слово.")]
        [SerializeField] private Sprite _windowField;

        [Tooltip("Масштаб кромки девятислойки. 0.25 — как во всех окнах игры.")]
        [SerializeField] private float _windowPixelsPerUnit = 0.25f;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(new WindowSkin(_windowFrame, _windowField, _windowPixelsPerUnit));

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

            // Опционально: иконка интеркома над радаром.
            var intercomIndicator = Object.FindFirstObjectByType<IntercomCallIndicatorUI>(FindObjectsInactive.Include);
            if (intercomIndicator != null)
            {
                builder.RegisterComponent(intercomIndicator);
            }

            var legend = Object.FindFirstObjectByType<ControlsLegendUI>(FindObjectsInactive.Include);
            if (legend != null)
            {
                builder.RegisterComponent(legend);
            }

            var relationships = Object.FindFirstObjectByType<RelationshipFeedUI>(FindObjectsInactive.Include);
            if (relationships != null)
            {
                builder.RegisterComponent(relationships);
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

            var confirmDialog = Object.FindFirstObjectByType<ConfirmDialogUI>(FindObjectsInactive.Include);
            if (confirmDialog != null)
            {
                builder.RegisterComponent(confirmDialog);
            }

            // Опционально: окна мини-игр (собираются кодом, в сцене только шрифт).
            var timingPuzzle = Object.FindFirstObjectByType<TimingPuzzleUI>(FindObjectsInactive.Include);
            if (timingPuzzle != null)
            {
                builder.RegisterComponent(timingPuzzle);
            }

            var typingPuzzle = Object.FindFirstObjectByType<TypingPuzzleUI>(FindObjectsInactive.Include);
            if (typingPuzzle != null)
            {
                builder.RegisterComponent(typingPuzzle);
            }

            var cutscenes = Object.FindFirstObjectByType<CutsceneController>(FindObjectsInactive.Include);
            if (cutscenes != null)
            {
                builder.RegisterComponent(cutscenes);
            }
        }
    }
}
