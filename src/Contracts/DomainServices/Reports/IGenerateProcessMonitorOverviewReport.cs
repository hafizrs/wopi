using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateProcessMonitorOverviewReport
    {
        Task<bool> PrepareTaskListReport(
            string filter,
            bool enableDateRange,
            DateTime? startDate,
            DateTime? endDate,
            PraxisClient client,
            ExcelPackage excel,
            ExportTaskListReportTranslation translation,
            int timezoneOffsetInMinutes
        );
    }
}