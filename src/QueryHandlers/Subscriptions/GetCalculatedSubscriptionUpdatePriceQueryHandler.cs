using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetCalculatedSubscriptionUpdatePriceQueryHandler : IQueryHandler<GetCalculatedSubscriptionUpdatePriceQuery, QueryHandlerResponse>
    {
        readonly ILogger<GetCalculatedSubscriptionUpdatePriceQueryHandler> _logger;
        private readonly ISubscriptionCalculationService _subscriptionCalculationService;
        public GetCalculatedSubscriptionUpdatePriceQueryHandler(ILogger<GetCalculatedSubscriptionUpdatePriceQueryHandler> logger,
            ISubscriptionCalculationService subscriptionCalculationService)
        {
            _logger = logger;
            _subscriptionCalculationService = subscriptionCalculationService;
        }
        public QueryHandlerResponse Handle(GetCalculatedSubscriptionUpdatePriceQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetCalculatedSubscriptionUpdatePriceQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                var numberOfUser = query.NumberOfUser;
                var subscriptionTypeSeedId = query.SubscriptionTypeSeedId;
                var durationOfSubscription = query.DurationOfSubscription;
                var clientId = query.ClientId;
                var numberOfSupportUnit = query.NumberOfSupportUnit;
                var additionalStorage = query.TotalAdditionalStorageInGigaBites;

                List<CalculatedPriceModel> results = _subscriptionCalculationService.CalculateSubscriptionUpdatePrice(numberOfUser, subscriptionTypeSeedId, durationOfSubscription, clientId);
                var praxisPaymentModuleSeed = _subscriptionCalculationService.GetPricingSubscriptionSeedData(subscriptionTypeSeedId);
                double? SupportSubscriptionCost = praxisPaymentModuleSeed.SupportSubscriptionPackage.PerUnitCost * numberOfSupportUnit;
                double? AdditionalStorageCost = praxisPaymentModuleSeed.StorageSubscriptionSeed.PricePerGigaBiteStorage * additionalStorage;

                return new QueryHandlerResponse()
                {
                    StatusCode = 0,
                    ErrorMessage = null,
                    Results = new
                    {
                        results,
                        SupportSubscriptionCost,
                        AdditionalStorageCost
                    },
                    TotalCount = results.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetCalculatedSubscriptionUpdatePriceQueryHandler), ex.Message, ex.StackTrace);
                return new QueryHandlerResponse
                {
                    StatusCode = 1,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<QueryHandlerResponse> HandleAsync(GetCalculatedSubscriptionUpdatePriceQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
