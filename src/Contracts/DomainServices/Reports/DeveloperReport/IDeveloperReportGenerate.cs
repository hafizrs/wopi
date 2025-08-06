using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport
{
    public interface IDeveloperReportGenerate
    {
        Task<bool> GenerateReport(string filter, PraxisClient client, ExcelPackage excel, DeveloperReportTranslation developerReportTranslation, string reportFileId);
    }
}
