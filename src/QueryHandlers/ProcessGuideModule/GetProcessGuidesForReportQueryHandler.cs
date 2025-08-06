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
    public class GetProcessGuidesForReportQueryHandler : IQueryHandler<GetReportQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetProcessGuidesForReportQueryHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;

        public GetProcessGuidesForReportQueryHandler(
            IPraxisProcessGuideService processGuideService,
            ILogger<GetProcessGuidesForReportQueryHandler> logger
        )
        {
            _processGuideService = processGuideService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetReportQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetReportQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetProcessGuidesForReportQueryHandler), JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var queryResult = await _processGuideService.PrepareProcessGuidePhotoDocumentationData(query);
                response.Results = queryResult.IsFileSizeExceeded ? null : queryResult.PraxisProcessGuidesForReport;
                response.ErrorMessage = queryResult.IsFileSizeExceeded ? "Total size of Image files exceeded 150 mb" : null;
                response.TotalCount = queryResult.PraxisProcessGuidesForReport.Count;
            }
            catch (Exception e)
            {
                response.ErrorMessage = e.Message;
                _logger.LogError("Error in {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                    nameof(GetProcessGuidesForReportQueryHandler), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetProcessGuidesForReportQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}