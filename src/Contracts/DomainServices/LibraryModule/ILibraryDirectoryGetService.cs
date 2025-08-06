using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryDirectoryGetService
    {
        Task<List<LibraryDirectoryResponse>> GetLibraryDirectories(LibraryDirectoryGetCommand command);
    }
}