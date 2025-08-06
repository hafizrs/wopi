using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IRiqsPediaQueryService
    {
        Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForRiqsPedia(GetPraxisClientsForRiqsPediaQuery query);
        Task<EntityQueryResponse<ProjectedUserResponse>> GetPraxisUserForRiqsPedia(GetPraxisUserForRiqsPediaQuery query);
    }
}
