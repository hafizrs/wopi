using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateTrainingDetailsReport
    {
        Task<bool> PrepareTrainingDetailsReport(string filter, PraxisClient client, PraxisTraining training,
            ExcelPackage excel, TrainingDetailsTranslation translation);
    }
}
