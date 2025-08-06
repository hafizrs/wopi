using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisRoomEvents
{
    public class PraxisRoomUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisRoom>>
    {
        private readonly ILogger<PraxisRoomUpdatedEventHandler> _logger;

        public PraxisRoomUpdatedEventHandler(
            ILogger<PraxisRoomUpdatedEventHandler> logger
        )
        {
            _logger = logger;
        }
        public bool Handle(GqlEvent<PraxisRoom> eventPayload)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisRoomUpdatedEventHandler -> {ErrorMessage}", e.Message);

            }

            return false;
        }
    }
}
