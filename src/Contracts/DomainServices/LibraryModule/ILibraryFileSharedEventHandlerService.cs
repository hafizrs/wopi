using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileSharedEventHandlerService
    {
        Task<bool> HandleLibraryFileSharedEvent(ObjectArtifactFileShareCommand command);
    }
}