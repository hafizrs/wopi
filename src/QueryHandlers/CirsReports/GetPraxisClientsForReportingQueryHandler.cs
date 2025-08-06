using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPraxisClientsForReportingQueryHandler : IQueryHandler<GetPraxisClientsForReportingQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetPraxisClientsForReportingQueryHandler> _logger;
        private readonly IPraxisClientsForReportingQueryService _service;

        public GetPraxisClientsForReportingQueryHandler(
            ILogger<GetPraxisClientsForReportingQueryHandler> logger,
            IPraxisClientsForReportingQueryService service)
        {
            _logger = logger;
            _service = service;
        }
        public QueryHandlerResponse Handle(GetPraxisClientsForReportingQuery query)
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

        public async Task<QueryHandlerResponse> HandleAsync(GetPraxisClientsForReportingQuery query)
        {
            if (query == null)
            {
                return CreateQueryHandlerResponse("Invalid Query: query is null");

            }

            var response = new QueryHandlerResponse();

            try
            {
                var queryResult = await _service.GetPraxisClientsForReport(query);
                response.Results = queryResult.Results;
                response.TotalCount = queryResult.TotalRecordCount;
                response.ErrorMessage = queryResult.ErrorMessage;
                response.StatusCode = queryResult.StatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetPraxisClientsForReportingQueryHandler), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by {HandlerName} with query: {Query}",
                nameof(GetPraxisClientsForReportingQueryHandler), query);
            return response;
        }
    }
}