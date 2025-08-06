using System.Collections.Generic;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateEquipmentReport
    {
        Task<bool> PrepareEquipmentListReport(string filter, bool enableDateRange, PraxisClient client,
            ExcelPackage excel, TranslationEpuimentList translationEquipmentList);
        List<string> GetEquipmentAssignedOrgAdmin(string organizationId);
    }
}
