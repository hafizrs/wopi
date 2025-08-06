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
    public class GetSubscriptionPricingCustomPackageQueryHandler : IQueryHandler<GetSubscriptionPricingCustomPackageQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionPricingCustomPackageQueryHandler> _logger;
        private readonly ISubscriptionPricingCustomPackageService _subscriptionPricingCustomPackageService;

        public GetSubscriptionPricingCustomPackageQueryHandler(
            ILogger<GetSubscriptionPricingCustomPackageQueryHandler> logger,
            ISubscriptionPricingCustomPackageService subscriptionPricingCustomPackageService)
        {
            _logger = logger;
            _subscriptionPricingCustomPackageService = subscriptionPricingCustomPackageService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionPricingCustomPackageQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionPricingCustomPackageQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetSubscriptionPricingCustomPackageQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _subscriptionPricingCustomPackageService.GetSubscriptionPricingCustomPackage(query.ItemId);

                response.StatusCode = 0;
                response.Results = results;
                response.TotalCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetSubscriptionPricingCustomPackageQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetSubscriptionPricingCustomPackageQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
