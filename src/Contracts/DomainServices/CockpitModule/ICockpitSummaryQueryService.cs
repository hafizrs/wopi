using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule
{
    public interface ICockpitSummaryQueryService
    {
        Task<QueryHandlerResponse> GetRiqsTaskCockpitSummary(GetCockpitSummaryQuery query);
        Task<QueryHandlerResponse> GetNewCirsReportsSummary(GetNewCirsReportsQuery query);
    }
}