using System.Collections.Generic;

namespace Game.Core
{
    public interface IUpgradeService
    {
        IReadOnlyList<UpgradeItemDefinition> Installed { get; }
        bool IsInstalled(UpgradeItemDefinition upgrade);
        bool TryInstall(UpgradeItemDefinition upgrade);
    }
}
