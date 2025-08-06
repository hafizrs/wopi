using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDeleteDmsArtifactUsageReferenceService
    {
        Task DeleteDataForClient(string clientId, string orgId = null);
    }
}