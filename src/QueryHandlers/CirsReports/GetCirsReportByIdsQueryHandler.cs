using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CirsReports
{
    public class GetCirsReportByIdsQueryHandler : AbstractQueryHandler<GetCirsReportByIdsQuery>
    {
        private readonly ILogger<GetCirsReportByIdsQueryHandler> _logger;
        private readonly ICirsReportQueryService _cirsReportQueryService;

        public GetCirsReportByIdsQueryHandler(
            ILogger<GetCirsReportByIdsQueryHandler> logger, 
            ICirsReportQueryService cirsReportQueryService)
        {
            _logger = logger;
            _cirsReportQueryService = cirsReportQueryService;
        }

        public override async Task<QueryHandlerResponse> HandleAsync(GetCirsReportByIdsQuery query)
        {
            var cirsReportResponse = await _cirsReportQueryService.GetReportsAsync(query);

            return new QueryHandlerResponse
            {
                Data = cirsReportResponse,
                StatusCode = 0
            };
        }
    }
}
