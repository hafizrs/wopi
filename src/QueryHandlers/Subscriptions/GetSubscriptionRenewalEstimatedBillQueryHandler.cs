using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetSubscriptionRenewalEstimatedBillQueryHandler : IQueryHandler<GetSubscriptionRenewalEstimatedBillQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionRenewalEstimatedBillQueryHandler> _logger;
        private readonly ISubscriptionRenewalEstimatedBillGenerationService _subscriptionRenewalEstimatedBillGenerationService;

        public GetSubscriptionRenewalEstimatedBillQueryHandler(
            ILogger<GetSubscriptionRenewalEstimatedBillQueryHandler> logger,
            ISubscriptionRenewalEstimatedBillGenerationService subscriptionRenewalEstimatedBillGenerationService)
        {
            _logger = logger;
            _subscriptionRenewalEstimatedBillGenerationService = subscriptionRenewalEstimatedBillGenerationService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionRenewalEstimatedBillQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionRenewalEstimatedBillQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetSubscriptionRenewalEstimatedBillQueryHandler), JsonConvert.SerializeObject(query));
            QueryHandlerResponse response = new QueryHandlerResponse();

            try
            {
                if(query != null)
                {
                    var results = await _subscriptionRenewalEstimatedBillGenerationService.GenerateSubscriptionRenewalEstimatedBill(
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
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error in {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                    nameof(GetSubscriptionRenewalEstimatedBillQueryHandler), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetSubscriptionRenewalEstimatedBillQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
