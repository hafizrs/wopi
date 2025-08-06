using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateTrainingReport
    {
        Task<bool> PrepareTrainingReport(string filter, PraxisClient client, ExcelPackage excel,
            TrainingReportTranslation translation);
    }
}
