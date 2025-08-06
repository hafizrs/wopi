using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IPraxisClientsForReportingQueryService
    {
        Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForReport(GetPraxisClientsForReportingQuery query);
    }
}
