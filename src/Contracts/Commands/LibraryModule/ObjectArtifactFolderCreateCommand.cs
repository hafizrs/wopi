using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactFolderCreateCommand: CreateFolderPayload
    {
        public bool IsAOrganizationFolder { get; set; }
    }
}