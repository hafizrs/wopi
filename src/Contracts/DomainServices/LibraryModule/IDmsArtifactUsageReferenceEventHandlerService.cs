using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDmsArtifactUsageReferenceEventHandlerService
    {
        Task InitiateArtifactUsageReferenceCreation(DmsArtifactUsageReferenceEventModel payload);
        Task InitiateArtifactUsageReferenceDeletion(DmsArtifactUsageReferenceDeleteEventModel payload);
    }
}