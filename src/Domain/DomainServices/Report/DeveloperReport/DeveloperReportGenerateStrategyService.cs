using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.DeveloperReport
{
    public class DeveloperReportGenerateStrategyService : IDeveloperReportGenerateStrategy
    {
        private readonly GenerateDeveloperReportForAllData _developerReportForAllData;
        private readonly GenerateDeveloperReportForClientSpecific _developerReportForClientSpecific;

        public DeveloperReportGenerateStrategyService(
            GenerateDeveloperReportForAllData developerReportForAllData,
            GenerateDeveloperReportForClientSpecific developerReportForClientSpecific)
        {
            _developerReportForAllData = developerReportForAllData;
            _developerReportForClientSpecific = developerReportForClientSpecific;
        }

        public IDeveloperReportGenerate GetReportType(bool isReportForAllData)
        {
            if (isReportForAllData)
                return _developerReportForAllData;

            return _developerReportForClientSpecific;
        }
    }
}