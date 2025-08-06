using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.RiskManagementEvents
{
    public class PraxisRiskCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisRisk>>
    {
        private readonly ILogger<PraxisRiskCreatedEventHandler> _logger;
        private readonly IPraxisRiskService _praxisRiskService;
        public PraxisRiskCreatedEventHandler(
            ILogger<PraxisRiskCreatedEventHandler> log,
            IPraxisRiskService praxisRiskService
        )
        {
            _logger = log;
            _praxisRiskService = praxisRiskService;
        }
        public bool Handle(GqlEvent<PraxisRisk> eventPayload)
        {
            try
            {
                string clientId = eventPayload.EntityData.ClientId;
                string riskId = eventPayload.EntityData.ItemId;

                _praxisRiskService.AddRowLevelSecurity(riskId, clientId);
                _praxisRiskService.UpdateAttachmentInReporting(riskId).GetAwaiter().GetResult();

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisRiskCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
