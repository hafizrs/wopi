using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFormHistoryService
    {
        Task<List<IDictionary<string, object>>> GetObjectArtifactFormHistory(ObjectArtifactFormHistoryQuery query);
    }
}