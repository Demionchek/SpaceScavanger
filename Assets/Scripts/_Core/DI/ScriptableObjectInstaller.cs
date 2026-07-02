using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Core
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);
    }
}
