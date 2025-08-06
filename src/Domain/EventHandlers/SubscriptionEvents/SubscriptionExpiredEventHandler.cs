using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.SubscriptionEvents
{
    public class SubscriptionExpiredEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<SubscriptionExpiredEventHandler> _logger;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        public SubscriptionExpiredEventHandler(
            ILogger<SubscriptionExpiredEventHandler> logger,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IOrganizationSubscriptionService organizationSubscriptionService)
        {
            _logger = logger;
            _departmentSubscriptionService = departmentSubscriptionService;
            _organizationSubscriptionService = organizationSubscriptionService;
        }
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(SubscriptionExpiredEventHandler), JsonConvert.SerializeObject(@event.JsonPayload));
            try
            {
                var payload = JsonConvert.DeserializeObject<SubscriptionExpiredEventModel>(@event.JsonPayload);

                if (!string.IsNullOrEmpty(payload.ClientId))
                {
                    await _departmentSubscriptionService.UpdateTokenBalanceOnSubscriptionExpiryAsync(payload.ClientId);
                }
                else if (!string.IsNullOrEmpty(payload.OrganizationId))
                {
                    await _organizationSubscriptionService.UpdateTokenBalanceOnSubscriptionExpiryAsync(payload.OrganizationId);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(SubscriptionExpiredEventHandler), JsonConvert.SerializeObject(@event.JsonPayload), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}
