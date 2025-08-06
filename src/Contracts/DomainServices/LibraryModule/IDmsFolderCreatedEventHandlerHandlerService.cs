using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDmsFolderCreatedEventHandlerHandlerService
    {
        Task<bool> HandleDmsFolderCreatedEvent(ObjectArtifactFolderCreateCommand fileUploadCommand);
        Task<bool> HandleDmsFolderListCreatedEvent(List<ObjectArtifactFolderCreateCommand> folderListCreatedCommand);
    }
}