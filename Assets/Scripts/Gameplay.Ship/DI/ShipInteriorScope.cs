using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Ship
{
    // Scope для additive-сцены ShipInterior. Платформенные компоненты интерьера
    // живут в этой сцене, поэтому их нельзя регистрировать из главного
    // GameLifetimeScope — его RegisterComponentInHierarchy ищет только в главной
    // сцене. Зависимости (EventBus, PlayerContext, InputActionAsset и т.п.)
    // резолвятся из родительского scope через parentReference (в инспекторе
    // Parent -> Game.Core.GameLifetimeScope).
    //
    // Имя НЕ оканчивается на "LifetimeScope" намеренно: иначе VContainer
    // ScriptTemplateProcessor затирает файл при первом импорте (vcontainer-reference §10).
    public sealed class ShipInteriorScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<CharacterMovementController>();
            builder.RegisterComponentInHierarchy<PlayerInteractor>();
            builder.RegisterComponentInHierarchy<WorkbenchComponent>();
            builder.RegisterComponentInHierarchy<ShipComputerComponent>();

            // Кресло пилота опционально: возврат в полёт есть и по клавише Dock.
            // Регистрируем только если оно реально в сцене — иначе строгий
            // RegisterComponentInHierarchy бросил бы исключение при входе в интерьер.
            var console = FindFirstObjectByType<PilotConsoleComponent>(FindObjectsInactive.Include);
            if (console != null)
            {
                builder.RegisterComponent(console);
            }
        }
    }
}
