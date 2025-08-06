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

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.SubscriptionEvents
{
    public class SubscriptionRenewEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<SubscriptionRenewEventHandler> _logger;
        private readonly IUpdateClientSubscriptionInformation _updateClientSubscriptionInformation;
        private readonly IRepository _repository;
        public SubscriptionRenewEventHandler(
            ILogger<SubscriptionRenewEventHandler> logger,
            IUpdateClientSubscriptionInformation updateClientSubscriptionInformation,
            IRepository repository)
        {
            _logger = logger;
            _updateClientSubscriptionInformation = updateClientSubscriptionInformation;
            _repository = repository;
        }
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(SubscriptionRenewEventHandler), JsonConvert.SerializeObject(@event.JsonPayload));
            try
            {
                var payload = JsonConvert.DeserializeObject<SubscriptionRenewEventModel>(@event.JsonPayload);
                var updateClientSubscriptionInformationCommand =  new UpdateClientSubscriptionInformationCommand
                {
                    ClientId = payload.ClientId,
                    OrganizationId = payload.OrganizationId
                };
               
                var subscriptionData = await _repository.GetItemAsync<PraxisClientSubscription>(x => x.ItemId == payload.SubscriptionId);

                if (subscriptionData != null)
                {
                    await _updateClientSubscriptionInformation.RenewSubscriptionIfAlreadyExpired(updateClientSubscriptionInformationCommand, subscriptionData, payload.NotificationId);
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(SubscriptionRenewEventHandler), JsonConvert.SerializeObject(@event.JsonPayload), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}
