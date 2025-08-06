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
    public class GetSubscriptionPriceConfigQueryHandler : IQueryHandler<GetSubscriptionPriceConfigQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionPriceConfigQueryHandler> _logger;
        private readonly ISubscriptionPriceConfigService _subscriptionPriceConfigService;

        public GetSubscriptionPriceConfigQueryHandler(
            ILogger<GetSubscriptionPriceConfigQueryHandler> logger,
            ISubscriptionPriceConfigService subscriptionPriceConfigService)
        {
            _logger = logger;
            _subscriptionPriceConfigService = subscriptionPriceConfigService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionPriceConfigQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionPriceConfigQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetSubscriptionPriceConfigQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _subscriptionPriceConfigService.GetSubscriptionPriceConfig();

                response.StatusCode = 0;
                response.Results = results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetSubscriptionPriceConfigQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetSubscriptionPriceConfigQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
