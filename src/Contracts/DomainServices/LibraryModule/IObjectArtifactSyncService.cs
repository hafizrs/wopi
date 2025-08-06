using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactSyncService
    {
        Task UpdateEntityDependencyAsync(List<string> artifactIds, ObjectArtifact updatedArtifact);
    }
}