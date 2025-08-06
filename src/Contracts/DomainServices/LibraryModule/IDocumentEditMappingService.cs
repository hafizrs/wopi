using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDocumentEditMappingService
    {
        Task CreateDocumentEditMappingRecord(ObjectArtifact objectArtifactData, string newHtmlFileId, string originalHtmlFileId, SecurityContext securityContext);
        Task<DocumentEditMappingRecord> GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(string objectArtifactId);
        Task<DocumentEditMappingRecord> GetDocumentMappingDraftHtmlFileInfo(string objectArtifactId);
        Task<DocumentEditMappingRecord> GetDocumentEditMappingRecordByDraftArtifact(string objectArtifactId);
        Task SaveObjectArtifactDocumentDraftedData(DmsFileUploadPayload dmsUploadPayload, string parentObjectArtifactId);
        Task<bool> DraftDocumentEditRecord(string objectArtifactId);
        Task<bool> IsVersionHistoryAvailable(string objectArtifactId, List<DocumentEditMappingRecord> documentMappingDatas = null);
        Task<bool> ProcessDocumentEditHtmlDocument(string objectArtifactId);
        Task<bool> IsAValidArtifactEditRequest(string artifactId, string fileType, CommandResponse response = null);
        Task UpdateDocumentEditMetaData(DocumentEditMappingRecord draftedData);
    }
}
