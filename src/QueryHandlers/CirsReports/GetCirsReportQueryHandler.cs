using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CirsReports
{
    public class GetCirsReportQueryHandler : AbstractQueryHandler<GetCirsReportQuery>
    {
        private readonly ILogger<GetCirsReportQueryHandler> _logger;
        private readonly ICirsReportQueryService _cirsReportQueryService;

        public GetCirsReportQueryHandler(
            ILogger<GetCirsReportQueryHandler> logger, 
            ICirsReportQueryService cirsReportQueryService)
        {
            _logger = logger;
            _cirsReportQueryService = cirsReportQueryService;
        }

        public override async Task<QueryHandlerResponse> HandleAsync(GetCirsReportQuery query)
        {
            try
            {
                var cirsReportResponses = await _cirsReportQueryService.GetReportsAsync(query);

                return new QueryHandlerResponse
                {
                    Data = cirsReportResponses,
                    StatusCode = 0,
                    TotalCount = cirsReportResponses.Count
                };
            }
            catch (System.Exception ex)
            {
                return new QueryHandlerResponse
                {
                    StatusCode = 1,
                    ErrorMessage = ex.Message,
                    Data = null,
                    Results = null,
                    TotalCount = 0
                };
            }
        }
    }
}
