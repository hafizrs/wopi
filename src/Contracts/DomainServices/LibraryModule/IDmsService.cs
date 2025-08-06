using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IDmsService
    {
        Task<bool> CreateFolder(ObjectArtifactFolderCreateCommand payload);
        Task<List<ObjectArtifactFolderCreateCommand>> CreateFolders(List<ObjectArtifactFolderCreateCommand> payloads);
        Task<string> UploadFile(ObjectArtifactFileUploadCommand payload, string token);
        Task<bool> DeleteObjectArtifact(string objectArtifactId, string organizationId = "");
        Task DeleteObjectArtifactsAsync(List<string> fileIds);
        IDictionary<string, MetaValuePair> PrepareMetaDataForDmsDocumentFileUpload(ObjectArtifact objectArtifact, bool alreadyDrafted, bool saveAsDraft, bool isNotifiedToCockpit = false);
        string GenerateVersionFromParentObjectArtifact(string parentVersion = null);
    }
}
