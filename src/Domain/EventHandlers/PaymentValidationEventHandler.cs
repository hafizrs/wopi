using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PaymentValidationEventHandler : IEventHandler<PaymentValidationResponse, bool>
    {
        private readonly ILogger<PaymentValidationEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly INotificationService _notificationService;

        public PaymentValidationEventHandler(
            ILogger<PaymentValidationEventHandler> logger,
            IRepository repository,
            INotificationService notificationService)
        {
            _logger = logger;
            _repository = repository;
            _notificationService = notificationService;
        }
        public bool Handle(PaymentValidationResponse @event)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(PaymentValidationResponse @event)
        {
            _logger.LogInformation("Enter Handler {HandlerName} with payload: {Payload}.", nameof(PaymentValidationEventHandler), JsonConvert.SerializeObject(@event));

            try
            {
                if (string.IsNullOrEmpty(@event.PaymentDetailId))
                {
                    _logger.LogError($"PaymentDetailId is null.");
                    await SendNotification(@event.PaymentDetailId, false);
                    return false;
                }
                if (@event.StatusCode != 0)
                {
                    _logger.LogError($"{@event.ErrorMessage}");
                    await SendNotification(@event.PaymentDetailId, false);
                    return false;
                }
                var existingPaymentDetail = _repository.GetItem<PaymentDetail>(p => p.ItemId == @event.PaymentDetailId);
                if (existingPaymentDetail != null)
                {
                    var existingClientSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.PaymentHistoryId == @event.PaymentDetailId);
                    if (existingClientSubscription != null)
                    {
                        existingClientSubscription.BillingAddress = new ClientBillingAddress { 
                            Street = existingPaymentDetail.BillingAddress.Street, 
                            PostalCode = existingPaymentDetail.BillingAddress.Zip, 
                            City = existingPaymentDetail.BillingAddress.City, 
                            CountryCode = existingPaymentDetail.BillingAddress.CountryCode 
                        };
                        existingClientSubscription.ResponsiblePerson = new ClientResponsiblePerson
                        {
                            Email = existingPaymentDetail.BillingAddress.Email,
                            FirstName = existingPaymentDetail.BillingAddress.FirstName,
                            LastName = existingPaymentDetail.BillingAddress.LastName,
                            Phone = existingPaymentDetail.BillingAddress.Phone
                        };

                        existingClientSubscription.PaymentMethod = existingPaymentDetail.PaymentMethod;
                        existingClientSubscription.CardDetails = new Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule.CardInformationModel
                        {
                            MaskedNumber = existingPaymentDetail.CardDetails?.MaskedNumber,
                            ExpYear = existingPaymentDetail.CardDetails.ExpYear,
                            ExpMonth = existingPaymentDetail.CardDetails.ExpMonth,
                            HolderName = existingPaymentDetail.CardDetails.HolderName,
                            HolderSegment = existingPaymentDetail.CardDetails.HolderSegment,
                            CountryCode = existingPaymentDetail.CardDetails.CountryCode,
                            HashValue = existingPaymentDetail.CardDetails.HashValue
                        };
                        existingClientSubscription.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                        await _repository.UpdateAsync<PraxisClientSubscription>(pm => pm.ItemId == existingClientSubscription.ItemId, existingClientSubscription);
                        _logger.LogInformation("Data has been successfully updated to {EntityName} entity with ItemId: {ItemId}.",
                            nameof(PraxisClientSubscription), existingClientSubscription.ItemId);
                        await SendNotification(@event.PaymentDetailId, true);
                    }
                    else
                    {
                        await SendNotification(@event.PaymentDetailId, false);
                    }
                    
                }
                else
                {
                    await SendNotification(@event.PaymentDetailId, false);
                }
            }
            catch (Exception ex)
            {
                await SendNotification(@event.PaymentDetailId, false);
                _logger.LogError("Exception occurred during update billing address information in {EntityName} entity with PaymentDetailId: {PaymentDetailId}. Exception Message: {ExceptionMessage}. Exception Details: {ExceptionDetails}.",
                    nameof(PraxisClientSubscription), @event.PaymentDetailId, ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by {HandlerName} with payload: {Payload}.", nameof(PaymentValidationEventHandler), JsonConvert.SerializeObject(@event));
            return true;
        }

        private async Task SendNotification(string subscriptionId, bool isSuccess)
        {
            var result = new
            {
                NotifiySubscriptionId = subscriptionId,
                Success = isSuccess
            };

            await _notificationService.PaymentNotification(isSuccess, subscriptionId, result, "PaymentValidation", "PaymentValidation");
        }
    }
}