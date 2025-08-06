using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport
{
    public interface IProcessGuideDetailReport
    {
        Task<bool> ExportReport(ExportProcessGuideDetailReportCommand command);
    }
}
