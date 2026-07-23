using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // Имя класса намеренно НЕ заканчивается на "LifetimeScope":
    // VContainer затирает новые файлы *LifetimeScope.cs шаблоном при импорте.
    public sealed class DialogueNpcScope : LifetimeScope
    {
        [SerializeField] private DialogueInteractable _dialogue;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_dialogue);
        }
    }
}
