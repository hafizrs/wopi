using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport
{
    public interface IProcessGuideCaseOverviewReport
    {
        Task<bool> ExportReport(ExportProcessGuideCaseOverviewReportCommand command);
    }
}
