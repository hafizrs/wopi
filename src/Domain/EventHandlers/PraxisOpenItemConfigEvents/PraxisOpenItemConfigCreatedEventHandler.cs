using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemConfigEvents
{
    public class PraxisOpenItemConfigCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisOpenItemConfig>>
    {
        private readonly IPraxisOpenItemService praxisOptenItemService;
        private readonly ILogger<PraxisOpenItemConfigCreatedEventHandler> _logger;
        public PraxisOpenItemConfigCreatedEventHandler(IPraxisOpenItemService praxisOptenItemService,
            ILogger<PraxisOpenItemConfigCreatedEventHandler> logger)
        {
            this.praxisOptenItemService = praxisOptenItemService;
            _logger = logger;
        }
        public bool Handle(GqlEvent<PraxisOpenItemConfig> eventPayload)
        {
            try
            {
                praxisOptenItemService.AddPraxisOpenItemConfigRowLevelSecurity(
                        eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId
                    );
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisOpenItemConfigUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
