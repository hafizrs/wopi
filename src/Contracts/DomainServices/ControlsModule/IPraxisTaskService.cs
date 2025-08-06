using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisTaskService
    {
        PraxisTask GetPraxisTask(string itemId);

        Task<PraxisTaskQueryResponse> GetDistinctTaskListByFilter(string filter, string sort, int pageNumber, int pageSize);

        Task<EntityQueryResponse<PraxisTask>> GetPraxisTasks(string filter, string sort, int pageNumber, int pageSize);

        void AddRowLevelSecurity(string itemId, string clientId);

        void RemoveRowLevelSecurity(string clientId);
        Task<bool> UpdatePraxisTaskAndSummaryStatus(string taskSummaryId, bool status);

        Task<PraxisTaskQueryResponse> GetDistictTaskListReportData(string filter, string sort);
        Task<EntityQueryResponse<PraxisTask>> GetOverviewReportData(string filter, string sort);
    }
}