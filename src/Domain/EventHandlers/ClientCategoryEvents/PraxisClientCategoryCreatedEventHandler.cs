using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientCategoryEvents
{
    public class PraxisClientCategoryCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisClientCategory>>
    {
        private readonly IPraxisClientCategoryService praxisClientCategoryService;
        private readonly ILogger<PraxisClientCategoryCreatedEventHandler> _logger;

        public PraxisClientCategoryCreatedEventHandler(
            IPraxisClientCategoryService praxisClientCategoryService,
            ILogger<PraxisClientCategoryCreatedEventHandler> logger
        )
        {
            this.praxisClientCategoryService = praxisClientCategoryService;
            _logger = logger;
        }

        public bool Handle(GqlEvent<PraxisClientCategory> eventPayload)
        {
            try
            {
                praxisClientCategoryService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisClientCategoryCreatedEventHandler -> {ErrorMessage}", e.Message);
            }
            return false;
        }
    }
}
