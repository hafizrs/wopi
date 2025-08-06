using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateDistinctTaskListReport
    {
        Task<bool> PrepareDistinctTaskListReport(PraxisClient client, ExcelPackage excel, string filter,
            TranslationDistinctTaskList translationDistinctTaskList);
    }
}
