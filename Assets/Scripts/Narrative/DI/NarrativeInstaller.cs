using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Narrative
{
    [CreateAssetMenu(menuName = "Game/Installers/Narrative Installer", fileName = "NarrativeInstaller")]
    public sealed class NarrativeInstaller : ScriptableObjectInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NarrativeDirector>();
        }
    }
}
