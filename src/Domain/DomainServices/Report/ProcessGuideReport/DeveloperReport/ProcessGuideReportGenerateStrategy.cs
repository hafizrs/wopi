using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport.DeveloperReport
{
    public class ProcessGuideReportGenerateStrategy : IProcessGuideReportGenerateStrategy
    {
        private readonly GenerateProcessGuideReportForAllClient _generateProcessGuideReportForAllClient;
        private readonly GenerateProcessGuideReportForClientSpecific _generateProcessGuideReportForClientSpecific;

        public ProcessGuideReportGenerateStrategy(
            GenerateProcessGuideReportForAllClient generateProcessGuideReportForAllClient,
            GenerateProcessGuideReportForClientSpecific generateProcessGuideReportForClientSpecific)
        {
            _generateProcessGuideReportForAllClient = generateProcessGuideReportForAllClient;
            _generateProcessGuideReportForClientSpecific = generateProcessGuideReportForClientSpecific;
        }
        public IProcessGuideReportGenerate GetReportType(bool isReportForAllData)
        {
            if (isReportForAllData)
                return _generateProcessGuideReportForAllClient;

            return _generateProcessGuideReportForClientSpecific;
        }
    }
}
