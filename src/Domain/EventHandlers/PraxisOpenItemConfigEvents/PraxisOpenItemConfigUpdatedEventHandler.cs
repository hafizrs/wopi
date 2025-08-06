using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemConfigEvents
{
    public class PraxisOpenItemConfigUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisOpenItemConfig>>
    {
        
        private readonly ILogger<PraxisOpenItemConfigUpdatedEventHandler> _logger;
        public PraxisOpenItemConfigUpdatedEventHandler(ILogger<PraxisOpenItemConfigUpdatedEventHandler> log)
        {
            _logger = log;
        }
        public bool Handle(GqlEvent<PraxisOpenItemConfig> eventPayload)
        {
            try
            {
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
