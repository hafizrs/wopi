using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileMovedEventHandlerService
    {
        Task<bool> HandleLibraryFileMovedEvent(string objectArtifactId);
    }
}