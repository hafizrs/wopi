using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFolderTreeSharedEventHandlerService
    {
        Task<bool> HandleLibraryFolderTreeSharedEvent(string[] objectArtifactIds);
    }
}