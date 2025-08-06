using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetSubscriptionsInfoQueryHandler : IQueryHandler<GetSubscriptionsInfoQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionsInfoQueryHandler> _logger;
        private readonly ISubscriptionUtilityService _subscriptionUtilityService;

        public GetSubscriptionsInfoQueryHandler(
            ILogger<GetSubscriptionsInfoQueryHandler> logger,
            ISubscriptionUtilityService subscriptionUtilityService)
        {
            _logger = logger;
            _subscriptionUtilityService = subscriptionUtilityService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionsInfoQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionsInfoQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetSubscriptionsInfoQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                SubscriptionsInfoResponse subscriptionsInfoResponse = await _subscriptionUtilityService.GetSubscriptionsInfo(query);
                response.Data = subscriptionsInfoResponse;
                response.StatusCode = 0;
                response.TotalCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetSubscriptionsInfoQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetSubscriptionsInfoQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
