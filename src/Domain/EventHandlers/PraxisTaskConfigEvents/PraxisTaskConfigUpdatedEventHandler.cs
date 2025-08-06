using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskConfigEvents
{
    public class PraxisTaskConfigUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTaskConfig>>
    {
        private readonly ILogger<PraxisTaskConfigUpdatedEventHandler> _logger;
        public PraxisTaskConfigUpdatedEventHandler(
            ILogger<PraxisTaskConfigUpdatedEventHandler> logger
        )
        {
            _logger = logger;
        }

        public bool Handle(GqlEvent<PraxisTaskConfig> eventPayload)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"PraxisTaskConfigUpdatedEventHandler -> {e.Message}");
            }

            return false;
        }
    }
}
