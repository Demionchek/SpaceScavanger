using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Ship
{
    [CreateAssetMenu(menuName = "Game/Installers/Ship Installer", fileName = "ShipInstaller")]
    public sealed class ShipInstaller : ScriptableObjectInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            // Платформенные компоненты интерьера переехали в additive-сцену
            // ShipInterior и регистрируются её ShipInteriorScope. Главный scope
            // их не ищет — в главной сцене их больше нет.
        }
    }
}
