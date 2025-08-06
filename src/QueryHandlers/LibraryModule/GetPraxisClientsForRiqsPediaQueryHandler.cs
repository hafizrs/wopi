using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPraxisClientsForRiqsPediaQueryHandler : IQueryHandler<GetPraxisClientsForRiqsPediaQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetPraxisClientsForRiqsPediaQueryHandler> _logger;
        private readonly IRiqsPediaQueryService _service;

        public GetPraxisClientsForRiqsPediaQueryHandler(
            ILogger<GetPraxisClientsForRiqsPediaQueryHandler> logger,
            IRiqsPediaQueryService service)
        {
            _logger = logger;
            _service = service;
        }
        public QueryHandlerResponse Handle(GetPraxisClientsForRiqsPediaQuery query)
        {
            return HandleAsync(query).Result;
        }

        private QueryHandlerResponse CreateQueryHandlerResponse(string message)
        {
            return new QueryHandlerResponse()
            {
                Results = null,
                ErrorMessage = message
            };
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetPraxisClientsForRiqsPediaQuery query)
        {
            if (query == null)
            {
                return CreateQueryHandlerResponse("Invalid Query: query is null");

            }

            var response = new QueryHandlerResponse();

            try
            {
                var queryResult = await _service.GetPraxisClientsForRiqsPedia(query);
                response.Results = queryResult.Results;
                response.TotalCount = queryResult.TotalRecordCount;
                response.ErrorMessage = queryResult.ErrorMessage;
                response.StatusCode = queryResult.StatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetPraxisClientsForRiqsPediaQueryHandler), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by {HandlerName} with query: {Query}",
                nameof(GetPraxisClientsForRiqsPediaQueryHandler), query);
            return response;
        }
    }
}
