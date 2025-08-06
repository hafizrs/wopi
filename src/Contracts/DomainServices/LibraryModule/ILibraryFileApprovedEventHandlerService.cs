using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileApprovedEventHandlerService
    {
        Task<bool> InitiateLibraryFileApprovedAfterEffects(string artifactId);
    }
}