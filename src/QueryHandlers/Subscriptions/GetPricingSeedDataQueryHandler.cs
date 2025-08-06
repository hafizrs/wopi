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
    public class GetPricingSeedDataQueryHandler : IQueryHandler<GetPricingSeedDataQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetPricingSeedDataQueryHandler> _logger;
        private readonly IPricingSeedDataService _pricingSeedDataService;

        public GetPricingSeedDataQueryHandler(
            ILogger<GetPricingSeedDataQueryHandler> logger,
            IPricingSeedDataService pricingSeedDataService)
        {
            _logger = logger;
            _pricingSeedDataService = pricingSeedDataService;
        }

        public QueryHandlerResponse Handle(GetPricingSeedDataQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetPricingSeedDataQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetPricingSeedDataQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _pricingSeedDataService.GetPricingSeedData();

                response.StatusCode = 0;
                response.Results = results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetPricingSeedDataQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetPricingSeedDataQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
