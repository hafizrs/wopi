using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactValidationService
    {
        bool ValidateUploadFile(ObjectArtifactFileUploadCommand uploadFileCommand, CommandResponse commandResponse, Workspace workSpace, StorageArea storageArea);
        bool ValidateCreateFolder(ObjectArtifactFolderCreateCommand createFolderCommand, CommandResponse commandResponse, Workspace workSpace, StorageArea storageArea, bool autoValid = false);
    }
}