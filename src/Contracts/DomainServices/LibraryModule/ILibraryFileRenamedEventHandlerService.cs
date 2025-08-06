using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileRenamedEventHandlerService
    {
        Task<bool> InitiateLibraryFileRenamedAfterEffects(string artifactId);
    }
}