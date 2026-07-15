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
            builder.RegisterComponentInHierarchy<CharacterMovementController>();
            builder.RegisterComponentInHierarchy<PlayerInteractor>();
            builder.RegisterComponentInHierarchy<WorkbenchComponent>();
            builder.RegisterComponentInHierarchy<ShipComputerComponent>();
        }
    }
}
