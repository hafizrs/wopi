using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryRightsUpdatedEventHandlerService
    {
        Task<bool> HandleLibraryRightsUpdatedEvent(RiqsLibraryControlMechanism control);
    }
}