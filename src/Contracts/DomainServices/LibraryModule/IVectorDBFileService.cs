using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IVectorDBFileService
    {
        Task<FileUploadToVectorDBResponse> UploadFile(List<FileUploadToVectorDBCommand> fileUploadToVectorDBCommand);
        Task<bool> DeleteFile(DeleteFileFromVectorDBCommand payload);
        Task HandleManualFileUpload(ObjectArtifact objectArtifact);
        Task UpdateManualFileUploadStatus(UpdateManualFileUploadStatusCommand command);
    }
}
