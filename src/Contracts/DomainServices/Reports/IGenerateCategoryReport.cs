using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateCategoryReport
    {
        Task<bool> PrepareCategoryReport(string filter, PraxisClient client, ExcelPackage excel,
            CategoryReportTranslation categoryReportTranslation);
    }
}
