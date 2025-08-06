namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport
{
    public interface IProcessGuideReportGenerateStrategy
    {
        IProcessGuideReportGenerate GetReportType(bool isReportForAllData);
    }
}
