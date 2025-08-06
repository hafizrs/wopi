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
    public class GetProcessGuideIdsQueryHandler : IQueryHandler<GetProcessGuideIdsQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetProcessGuideIdsQueryHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;

        public GetProcessGuideIdsQueryHandler(
            IPraxisProcessGuideService processGuideService,
            ILogger<GetProcessGuideIdsQueryHandler> logger
        )
        {
            _processGuideService = processGuideService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetProcessGuideIdsQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<QueryHandlerResponse> HandleAsync(GetProcessGuideIdsQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.", nameof(GetProcessGuideIdsQueryHandler),
                JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var queryResult = _processGuideService.GetProcessGuideIds(query.processGuideConfigId);
                response.Data = queryResult;
                response.StatusCode = queryResult == null ? 1 : 0;
                response.TotalCount = queryResult?.Count ?? 0;
            }
            catch (Exception e)
            {
                response.StatusCode = 1;
                response.ErrorMessage = e.Message;
                _logger.LogError("Error in {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                    nameof(GetProcessGuideIdsQueryHandler), e.Message, e.StackTrace);
            }

            return Task.FromResult(response);
        }
    }
}