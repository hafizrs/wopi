
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactFileUploadCommand: DmsFileUploadPayload
    {
        public bool IsUploadFromInterface { get; set; } = false;
    }
}