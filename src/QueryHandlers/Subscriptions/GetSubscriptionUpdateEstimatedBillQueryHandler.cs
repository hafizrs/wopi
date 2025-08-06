using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetSubscriptionUpdateEstimatedBillQueryHandler : IQueryHandler<GetSubscriptionUpdateEstimatedBillQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionUpdateEstimatedBillQueryHandler> _logger;
        private readonly ISubscriptionUpdateEstimatedBillGenerationService _subscriptionUpdateEstimatedBillGenerationService;

        public GetSubscriptionUpdateEstimatedBillQueryHandler(
            ILogger<GetSubscriptionUpdateEstimatedBillQueryHandler> logger,
            ISubscriptionUpdateEstimatedBillGenerationService subscriptionUpdateEstimatedBillGenerationService)
        {
            _logger = logger;
            _subscriptionUpdateEstimatedBillGenerationService = subscriptionUpdateEstimatedBillGenerationService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionUpdateEstimatedBillQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionUpdateEstimatedBillQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetSubscriptionUpdateEstimatedBillQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _subscriptionUpdateEstimatedBillGenerationService.GenerateSubscriptionUpdateEstimatedBill(
                    query.OrganizationId,
                    query.ClientId,
                    query.SubscriptionId,
                    query.SubscriptionTypeSeedId,
                    query.NumberOfUser,
                    query.DurationOfSubscription,
                    query.NumberOfSupportUnit,
                    query.TotalAdditionalStorageInGigaBites,
                    query.TotalAdditionalTokenInMillion,
                    query.TotalAdditionalManualTokenInMillion);

                response.StatusCode = 0;
                response.Results = results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetSubscriptionUpdateEstimatedBillQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetSubscriptionUpdateEstimatedBillQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
