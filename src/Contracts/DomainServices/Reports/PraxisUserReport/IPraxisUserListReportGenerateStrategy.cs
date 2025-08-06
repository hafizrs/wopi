namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport
{
    public interface IPraxisUserListReportGenerateStrategy
    {
        IPraxisUserListReportGenerate GetReportType(bool isReportForAllData);
    }
}