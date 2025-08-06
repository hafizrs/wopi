using System.Threading.Tasks;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport
{
    public interface IPraxisUserListReportGenerate
    {
        Task<bool> GenerateReport(ExcelPackage excel, ExportPraxisUserListReportCommand command);
    }
}