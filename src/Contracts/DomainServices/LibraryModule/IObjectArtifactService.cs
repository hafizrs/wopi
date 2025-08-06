using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactService
    {
        Task<SearchResult> InitiateObjectArtifactRenameAsync(ObjectArtifactRenameCommand command);
        Task<CommandResponse> InitiateObjectArtifactFileUploadAsync(ObjectArtifactFileUploadCommand uploadFileCommand);
        Task<CommandResponse> InitiateObjectArtifactFolderCreateAsync(ObjectArtifactFolderCreateCommand createFolderCommand);
        Task<List<ObjectArtifactFolderCreateCommand>> InitiateObjectArtifactFolderListCreateAsync(List<ObjectArtifactFolderCreateCommand> createFolderListCommand);
    }
}