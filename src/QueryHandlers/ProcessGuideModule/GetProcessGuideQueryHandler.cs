using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetProcessGuideQueryHandler : IQueryHandler<GetProcessGuideQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetProcessGuideQueryHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;

        public GetProcessGuideQueryHandler(
            IPraxisProcessGuideService processGuideService,
            ILogger<GetProcessGuideQueryHandler> logger
        )
        {
            _processGuideService = processGuideService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetProcessGuideQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetProcessGuideQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.", nameof(GetProcessGuideQueryHandler),
                JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var queryResult = await _processGuideService.GetPraxisProcessGuide(query);
                response.Results = queryResult.Results;
                response.TotalCount = queryResult.TotalRecordCount;
                response.ErrorMessage = queryResult.ErrorMessage;
                response.StatusCode = queryResult.StatusCode;
            }
            catch (Exception e)
            {
                response.ErrorMessage = e.Message;
                _logger.LogError("Error in {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                    nameof(GetProcessGuideQueryHandler), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetProcessGuideQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}