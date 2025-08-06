using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPraxisRiskChartDataQueryHandler : IQueryHandler<GetPraxisRiskChartDataQuery, QueryHandlerResponse>
    {
        private readonly IPraxisRiskService _riskService;
        private readonly ILogger<GetPraxisRiskChartDataQueryHandler> _logger;

        public GetPraxisRiskChartDataQueryHandler(
            IPraxisRiskService riskService,
            ILogger<GetPraxisRiskChartDataQueryHandler> logger
        )
        {
            _riskService = riskService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetPraxisRiskChartDataQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetPraxisRiskChartDataQuery query)
        {
            var queryResponse = new QueryHandlerResponse();
            try
            {
                queryResponse.Results = await _riskService.GetPraxisRiskChartData(query);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                queryResponse.StatusCode = 1;
                queryResponse.ErrorMessage = e.StackTrace;
            }

            return queryResponse;
        }
    }
}