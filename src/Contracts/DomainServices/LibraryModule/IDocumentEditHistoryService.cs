using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDocumentEditHistoryService
    {
        IEnumerable<DocumentEditHistoryResponse> GetDocumentEditHistory(string objectArtifactId);
        Task<List<DocumentEditMappingRecord>> GeneratePreviousHistoryByArtifactId(string artifactId);
        List<DocumentEditMappingRecord> GenerateAllLinkedArtifactsByArtifactIds(List<string> artifactIds);
    }
}
