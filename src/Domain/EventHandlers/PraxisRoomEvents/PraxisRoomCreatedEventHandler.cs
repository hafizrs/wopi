using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisRoomEvents
{
    public class PraxisRoomCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisRoom>>
    {
        private readonly ILogger<PraxisRoomCreatedEventHandler> _logger;
        private readonly IPraxisRoomService praxisRoomService;

        public PraxisRoomCreatedEventHandler(
            ILogger<PraxisRoomCreatedEventHandler> logger,
            IPraxisRoomService praxisRoomService
        )
        {
            this._logger = logger;
            this.praxisRoomService = praxisRoomService;
        }
        public bool Handle(GqlEvent<PraxisRoom> eventPayload)
        {
            try
            {
                praxisRoomService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisRoomCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
