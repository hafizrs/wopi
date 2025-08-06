using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class SubscriptionUpdateEstimatedBillGenerationService : ISubscriptionUpdateEstimatedBillGenerationService
    {
        private readonly ILogger<SubscriptionUpdateEstimatedBillGenerationService> _logger;
        private readonly ISubscriptionCalculationService _subscriptionEstimatedBillCalculationService;

        public SubscriptionUpdateEstimatedBillGenerationService(
            ILogger<SubscriptionUpdateEstimatedBillGenerationService> logger,
            ISubscriptionCalculationService subscriptionEstimatedBillCalculationService)
        {
            _logger = logger;
            _subscriptionEstimatedBillCalculationService = subscriptionEstimatedBillCalculationService;
        }

        public async Task<SubscriptionEstimatedBillResponse> GenerateSubscriptionUpdateEstimatedBill(
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
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionUpdateEstimatedBillGenerationService));

            var response = new SubscriptionEstimatedBillResponse();

            try
            {
                var praxisPaymentModuleSeed = await _subscriptionEstimatedBillCalculationService.GetSubscriptionSeedData(subscriptionTypeSeedId);
                if (praxisPaymentModuleSeed != null)
                {
                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        response.PackageCosts = _subscriptionEstimatedBillCalculationService.GetCompletePackageSubscriptionUpdatePrice(
                           organizationId,
                           subscriptionTypeSeedId,
                           numberOfUser,
                           durationOfSubscription);
                    }
                    else if (!string.IsNullOrEmpty(clientId))
                    {
                        response.PackageCosts = _subscriptionEstimatedBillCalculationService.GetCompletePackageSubscriptionUpdatePriceForClient(
                           clientId,
                           subscriptionTypeSeedId,
                           numberOfUser,
                           durationOfSubscription);
                    }

                    response.SupportSubscriptionCost = (double)(praxisPaymentModuleSeed.SupportSubscriptionPackage.PerUnitCost * numberOfSupportUnit);
                    response.AdditionalStorageCost = (double)(praxisPaymentModuleSeed.StorageSubscriptionSeed.PricePerGigaBiteStorage * additionalStorage);
                    response.AdditionalTokenCost = (double)(praxisPaymentModuleSeed.TokenSubscriptionSeed.PricePerMillionToken * additionalToken);
                    response.AdditionalManualTokenCost = (double)(praxisPaymentModuleSeed.TokenSubscriptionSeed.PricePerMillionToken * additionalManualToken);
                    response =_subscriptionEstimatedBillCalculationService.CalculateOtherPropertiesOfBillCosts(praxisPaymentModuleSeed, response, subscriptionId, organizationId, durationOfSubscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionUpdateEstimatedBillGenerationService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionUpdateEstimatedBillGenerationService));

            return response;
        }
    }
}