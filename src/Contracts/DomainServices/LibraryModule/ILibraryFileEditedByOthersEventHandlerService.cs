using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileEditedByOthersEventHandlerService
    {
        Task<bool> HandleLibraryFileEditedByOthersEvent(string objectArtifactId);
    }
}