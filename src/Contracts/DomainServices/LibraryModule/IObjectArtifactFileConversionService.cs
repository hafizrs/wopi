using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFileConversionService
    {
        Task MakeACopyHtmlFileId(GetHtmlFileIdFromObjectArtifactDocumentCommand command);
        Task ProcessObjectArtifactHtmlDocument(ProcessDraftedObjectArtifactDocumentCommand command);
        Task<string> ConvertToHtmlAndUpload(string fileId);
    }
}
