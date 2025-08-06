using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFolderShareService
    {
        Task<SearchResult> InitiateSharebjectArtifactFolder(ObjectArtifactFolderShareCommand command);
    }
}