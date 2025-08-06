using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateOpenItemReport
    {
        Task<bool> PrepareOpenItemReport(string filter, bool enableDateRange, DateTime? startDate, DateTime? endDate,
            PraxisClient client, ExcelPackage excel, TranslationOpenItem translation, int timezoneOffsetInMinutes);
    }
}
