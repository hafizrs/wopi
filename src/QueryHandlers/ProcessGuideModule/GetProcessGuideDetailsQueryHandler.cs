using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetProcessGuideDetailsQueryHandler : IQueryHandler<GetProcessGuideDetailsQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetProcessGuideDetailsQueryHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;

        public GetProcessGuideDetailsQueryHandler(
            IPraxisProcessGuideService processGuideService,
            ILogger<GetProcessGuideDetailsQueryHandler> logger
        )
        {
            _processGuideService = processGuideService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetProcessGuideDetailsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetProcessGuideDetailsQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.", nameof(GetProcessGuideDetailsQueryHandler),
                JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var queryResult = (await _processGuideService.GetPraxisProcessGuideDetails(
                    query.ProcessGuideIds.ToList(), query.PraxisClientId, query.TimezoneOffsetInMinutes
                )).Results;
                response.Results = queryResult;
                response.TotalCount = queryResult.Count();
            }
            catch (Exception e)
            {
                response.ErrorMessage = e.Message;
                _logger.LogError("Error in {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                    nameof(GetProcessGuideDetailsQueryHandler), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.", nameof(GetProcessGuideDetailsQueryHandler),
                JsonConvert.SerializeObject(response));

            return response;
        }
    }
}