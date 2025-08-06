using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CockpitModule
{
    public class GetNewCirsReportsQueryHandler : IQueryHandler<GetNewCirsReportsQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetNewCirsReportsQueryHandler> _logger;
        private readonly ICockpitSummaryQueryService _cockpitSummaryQueryService;

        public GetNewCirsReportsQueryHandler(ILogger<GetNewCirsReportsQueryHandler> logger, ICockpitSummaryQueryService cockpitSummaryQueryService)
        {
            _logger = logger;
            _cockpitSummaryQueryService = cockpitSummaryQueryService;
        }
        public QueryHandlerResponse Handle(GetNewCirsReportsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetNewCirsReportsQuery query)
        {
            _logger.LogInformation("Entered {HandlerName} with query: {@Query}", nameof(GetNewCirsReportsQueryHandler), JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                response = await _cockpitSummaryQueryService.GetNewCirsReportsSummary(query);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred in {HandlerName} with query: {@Query}", nameof(GetNewCirsReportsQueryHandler), JsonConvert.SerializeObject(query));
                response.ErrorMessage = e.Message;
            }
            return response;
        }
    }
}
