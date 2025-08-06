using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactMoveService
    {
        Task<bool> InitiateObjectArtifactMoveAsync(ObjectArtifactMoveCommand command);
        Task MoveChildObjectArtifactsAsync(string parentId, List<string> artifactIds);
    }
}