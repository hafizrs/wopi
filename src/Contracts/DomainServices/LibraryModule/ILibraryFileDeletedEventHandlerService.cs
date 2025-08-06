using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryFileDeletedEventHandlerService
    {
        Task<bool> HandleLibraryFileDeletedEvent(List<string> artifactIds);
    }
}