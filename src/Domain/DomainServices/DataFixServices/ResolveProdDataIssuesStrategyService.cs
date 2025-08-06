using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class ResolveProdDataIssuesStrategyService : IResolveProdDataIssuesStrategyService
    {
        private readonly OldDataFixService _oldDataFixService;
        private readonly DmsDataCorrectionService _dmsDataCorrectionService;

        public ResolveProdDataIssuesStrategyService(
            OldDataFixService oldDataFixService,
            DmsDataCorrectionService dmsDataCorrectionService)
        {
            _oldDataFixService = oldDataFixService;
            _dmsDataCorrectionService = dmsDataCorrectionService;
        }

        public IResolveProdDataIssuesService GetDataFixService(ResolveProdDataIssuesCommand command)
        {
            return command.Context switch
            {
                "IsFixingOldData" => _oldDataFixService,
                "DmsDataFix" => _dmsDataCorrectionService,
                _ => null,
            };
        }
    }
}