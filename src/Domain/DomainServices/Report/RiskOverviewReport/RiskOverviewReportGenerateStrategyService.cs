using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.RiskOverviewReport
{
    public class RiskOverviewReportGenerateStrategyService: IRiskOverviewReportGenerateStrategy
    {
        private readonly GenerateRiskOverviewReportForSingleClient _generateRiskOverviewReportForSingleClient;
        private readonly GenerateRiskOverviewReportForMultipleClient _generateRiskOverviewReportForMultipleClient;

        public RiskOverviewReportGenerateStrategyService(
            GenerateRiskOverviewReportForSingleClient generateRiskOverviewReportForSingleClient,
            GenerateRiskOverviewReportForMultipleClient generateRiskOverviewReportForMultipleClient    
        )
        {
            _generateRiskOverviewReportForSingleClient = generateRiskOverviewReportForSingleClient;
            _generateRiskOverviewReportForMultipleClient = generateRiskOverviewReportForMultipleClient;
        }
        
        public IGenerateRiskOverviewReport GetReportType(bool isReportForAllData)
        {
            return isReportForAllData
                ? _generateRiskOverviewReportForMultipleClient
                : (IGenerateRiskOverviewReport) _generateRiskOverviewReportForSingleClient;
        }
    }
}