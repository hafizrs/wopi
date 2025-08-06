using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactQueryService
    {
        Task<List<ObjectArtifactSimpleResponse>> GetObjectArtifacts(ObjectArtifactQuery query);
    }
}