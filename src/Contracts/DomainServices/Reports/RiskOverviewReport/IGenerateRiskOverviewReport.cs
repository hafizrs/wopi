using System.Threading.Tasks;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport
{
    public interface IGenerateRiskOverviewReport
    {
        Task<bool> GenerateReport(ExcelPackage excel, ExportRiskOverviewReportCommand command);
    }
}