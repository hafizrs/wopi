using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class SubscriptionRenewalEstimatedBillGenerationService : ISubscriptionRenewalEstimatedBillGenerationService
    {
        private readonly ILogger<SubscriptionRenewalEstimatedBillGenerationService> _logger;
        private readonly ISubscriptionCalculationService _subscriptionEstimatedBillCalculationService;
        private readonly IRepository _repository;

        public SubscriptionRenewalEstimatedBillGenerationService(
            ILogger<SubscriptionRenewalEstimatedBillGenerationService> logger,
            ISubscriptionCalculationService subscriptionEstimatedBillCalculationService,
            IRepository repository
        )
        {
            _logger = logger;
            _subscriptionEstimatedBillCalculationService = subscriptionEstimatedBillCalculationService;
            _repository = repository;
        }

        public async Task<SubscriptionEstimatedBillResponse> GenerateSubscriptionRenewalEstimatedBill(
            string organizationId,
            string clientId,
            string subscriptionId,
            string subscriptionTypeSeedId,
            int numberOfUser,
            int durationOfSubscription,
            int numberOfSupportUnit,
            double additionalStorage,
            double additionalToken,
            double additionalManualToken)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionRenewalEstimatedBillGenerationService));

            var response = new SubscriptionEstimatedBillResponse();

            try
            {
                var praxisPaymentModuleSeed = await _subscriptionEstimatedBillCalculationService.GetSubscriptionSeedData(subscriptionTypeSeedId);
                if (praxisPaymentModuleSeed != null)
                {
                    response.PackageCosts = _subscriptionEstimatedBillCalculationService.CalculateSubscriptionPrice(numberOfUser, subscriptionTypeSeedId, durationOfSubscription);
                    response.SupportSubscriptionCost = (double)(praxisPaymentModuleSeed.SupportSubscriptionPackage.PerUnitCost * numberOfSupportUnit);    
                    response.AdditionalStorageCost = (double)(praxisPaymentModuleSeed.StorageSubscriptionSeed.PricePerGigaBiteStorage * additionalStorage);
                    response.AdditionalTokenCost = (double)(praxisPaymentModuleSeed.TokenSubscriptionSeed.PricePerMillionToken * additionalToken);
                    response.AdditionalManualTokenCost = (double)(praxisPaymentModuleSeed.TokenSubscriptionSeed.PricePerMillionToken * additionalManualToken);
                    response = _subscriptionEstimatedBillCalculationService.CalculateOtherPropertiesOfBillCosts(praxisPaymentModuleSeed, response, subscriptionId, organizationId, durationOfSubscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionRenewalEstimatedBillGenerationService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionRenewalEstimatedBillGenerationService));

            return response;
        }
    }
}