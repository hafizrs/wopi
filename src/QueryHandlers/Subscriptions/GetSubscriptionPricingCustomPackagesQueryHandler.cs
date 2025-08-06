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
    public class GetSubscriptionPricingCustomPackagesQueryHandler : IQueryHandler<GetSubscriptionPricingCustomPackagesQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSubscriptionPricingCustomPackagesQueryHandler> _logger;
        private readonly ISubscriptionPricingCustomPackageService _subscriptionPricingCustomPackageService;

        public GetSubscriptionPricingCustomPackagesQueryHandler(
            ILogger<GetSubscriptionPricingCustomPackagesQueryHandler> logger,
            ISubscriptionPricingCustomPackageService subscriptionPricingCustomPackageService)
        {
            _logger = logger;
            _subscriptionPricingCustomPackageService = subscriptionPricingCustomPackageService;
        }

        public QueryHandlerResponse Handle(GetSubscriptionPricingCustomPackagesQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSubscriptionPricingCustomPackagesQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetSubscriptionPricingCustomPackagesQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _subscriptionPricingCustomPackageService.GetSubscriptionPricingCustomPackages();

                response.StatusCode = 0;
                response.Results = results;
                response.TotalCount = results.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetSubscriptionPricingCustomPackagesQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetSubscriptionPricingCustomPackagesQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
