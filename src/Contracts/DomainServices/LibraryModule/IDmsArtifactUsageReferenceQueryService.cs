using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDmsArtifactUsageReferenceQueryService
    {
        Task<List<DmsArtifactUsageReferenceDto>> GetDmsArtifactUsageReference(string objectArtifactId, string clientId);
    }
}