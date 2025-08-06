using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFolderSharedEventHandlerService
    {
        Task<bool> HandleObjectArtifactFolderSharedEvent(ObjectArtifactFileShareCommand command);
    }
}