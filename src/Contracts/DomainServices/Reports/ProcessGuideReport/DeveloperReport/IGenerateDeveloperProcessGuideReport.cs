using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport
{
    public interface IGenerateDeveloperProcessGuideReport
    {
        Task<bool> GenerateReport(ExportProcessGuideReportForDeveloperCommand command);
    }
}
