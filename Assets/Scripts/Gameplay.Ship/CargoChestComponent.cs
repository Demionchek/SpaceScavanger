using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Ship
{
    // Сундук в трюме: открывает тот же инвентарь, что и клавиша. Своего окна у
    // него нет — трюм один на корабль, и вторая его копия разошлась бы с первой.
    //
    // Шина берётся из контекста, который и так приходит в Interact, а не из
    // инъекции: ShipInteriorScope перечисляет интерьерные компоненты поимённо, и
    // новый с [Inject] молча остался бы с пустыми полями.
    public sealed class CargoChestComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Cargo hold";

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            ctx.EventBus.Publish(new InventoryWindowRequestedEvent());
        }
    }
}
