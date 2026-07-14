using System.Collections.Generic;

namespace Game.Core
{
    public interface IUpgradeService
    {
        IReadOnlyList<UpgradeItemDefinition> Installed { get; }
        bool TryInstall(UpgradeItemDefinition upgrade);
    }
}
