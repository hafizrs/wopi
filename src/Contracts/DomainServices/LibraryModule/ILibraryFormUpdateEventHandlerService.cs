using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFormUpdateEventHandlerService
    {
        Task<bool> InitiateLibraryFormUpdateAfterEffects(string artifactId);
    }
}