using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport
{
    public class PraxisUserListReportGenerateStrategyService : IPraxisUserListReportGenerateStrategy
    {
        private readonly GeneratePraxisUserListReportForAllData _praxisUserListReportForAllData;
        private readonly GeneratePraxisUserListReportForSpecificClient _praxisUserListReportForClientSpecific;

        public PraxisUserListReportGenerateStrategyService(
            GeneratePraxisUserListReportForAllData praxisUserListReportForAllData,
            GeneratePraxisUserListReportForSpecificClient praxisUserListReportForClientSpecific)
        {
            _praxisUserListReportForAllData = praxisUserListReportForAllData;
            _praxisUserListReportForClientSpecific = praxisUserListReportForClientSpecific;
        }

        public IPraxisUserListReportGenerate GetReportType(bool isReportForAllData)
        {
            return isReportForAllData
                ? _praxisUserListReportForAllData
                : (IPraxisUserListReportGenerate) _praxisUserListReportForClientSpecific;
        }
    }
}