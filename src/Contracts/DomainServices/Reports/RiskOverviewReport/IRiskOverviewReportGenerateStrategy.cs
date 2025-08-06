namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport
{
    public interface IRiskOverviewReportGenerateStrategy
    {
        IGenerateRiskOverviewReport GetReportType(bool isReportForAllData);

    }
}