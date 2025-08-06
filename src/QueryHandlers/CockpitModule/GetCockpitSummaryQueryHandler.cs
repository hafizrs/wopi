using System;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CockpitModule
{
    public class GetCockpitSummaryQueryHandler : IQueryHandler<GetCockpitSummaryQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetCockpitSummaryQueryHandler> _logger;
        private readonly ICockpitSummaryQueryService _cockpitSummaryQueryService;

        public GetCockpitSummaryQueryHandler(ILogger<GetCockpitSummaryQueryHandler> logger, ICockpitSummaryQueryService cockpitSummaryQueryService)
        {
            _logger = logger;
            _cockpitSummaryQueryService = cockpitSummaryQueryService;
        }
        public QueryHandlerResponse Handle(GetCockpitSummaryQuery query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetCockpitSummaryQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetCockpitSummaryQueryHandler), query);
            
            var response = new QueryHandlerResponse();
            try
            {
                response = await _cockpitSummaryQueryService.GetRiqsTaskCockpitSummary(query);
            }
            catch (Exception e)
            {
                _logger.LogError("Error in {HandlerName}. Error Message -> {Message}. Error Details -> {StackTrace}",
                    nameof(GetCockpitSummaryQueryHandler), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled by {HandlerName} with query: {Query}.",
                nameof(GetCockpitSummaryQueryHandler), query);
            return response;
        }
    }
}