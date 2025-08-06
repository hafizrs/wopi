using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskConfigEvents
{
    public class PraxisTaskConfigCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTaskConfig>>
    {
        private readonly IPraxisTaskConfigService praxisTaskConfigService;
        private readonly ILogger<PraxisTaskConfigCreatedEventHandler> _logger;
        private readonly IPraxisFormService praxisFormService;

        public PraxisTaskConfigCreatedEventHandler(
            IPraxisTaskConfigService praxisTaskConfigService,
            ILogger<PraxisTaskConfigCreatedEventHandler> logger,
            IPraxisFormService praxisFormService
        )
        {
            this.praxisTaskConfigService = praxisTaskConfigService;
            this.praxisFormService = praxisFormService;
            _logger = logger;
        }

        public bool Handle(GqlEvent<PraxisTaskConfig> eventPayload)
       {
            try
            {
                praxisTaskConfigService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId);
                praxisFormService.UpdatePraxisForm("PraxisTaskConfig", eventPayload.EntityData.ItemId, eventPayload.EntityData.FormIds,eventPayload.EntityData.ClientId);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisTaskConfigCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
