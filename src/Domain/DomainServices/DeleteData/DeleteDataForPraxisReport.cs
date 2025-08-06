using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForPraxisReport: IDeleteDataByCollectionSpecific
    {
        private readonly IPraxisReportService _praxisReportService;

        public DeleteDataForPraxisReport( IPraxisReportService praxisReportService)
        {
            _praxisReportService = praxisReportService;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            return await _praxisReportService.DeletePraxisReport(itemId);
        }
    }
}