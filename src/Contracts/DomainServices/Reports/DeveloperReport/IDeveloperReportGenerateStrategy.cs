namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport
{
    public interface IDeveloperReportGenerateStrategy
    {
        IDeveloperReportGenerate GetReportType(bool isReportForAllData);
    }
}