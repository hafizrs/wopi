using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateSuppliersReport
    {
        Task<bool> PrepareSuppliersReport(string filter, PraxisClient client, ExcelPackage excel,
         SuppliersReportTranslation suppliersReportTranslation, Dictionary<string, string> supplierKeyNameTranslation);
    }
}
