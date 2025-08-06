using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IDocumentKeywordService
    {
        Task UpdateObjectArtifactKeywords(string objectArtifactId);
        Task UpdateKeywords(string[] keywords, string objectArtifactId);
        Task<string[]> GetKeywordValues(string organisationId);
    }
}
