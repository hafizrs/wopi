using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CockpitModule
{
    public class CockpitDocumentActivityMetricsQueryHandler : IQueryHandler<CockpitDocumentActivityMetricsQuery, QueryHandlerResponse>
    {
        private readonly ILogger<CockpitDocumentActivityMetricsQueryHandler> _logger;
        private readonly ICockpitDocumentActivityMetricsQueryService _cockpitDocumentActivityMetricsQueryService;
        private readonly ISecurityHelperService _securityHelperService;

        public CockpitDocumentActivityMetricsQueryHandler(
            ILogger<CockpitDocumentActivityMetricsQueryHandler> logger,
            ICockpitDocumentActivityMetricsQueryService cockpitDocumentActivityMetricsQueryService,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _cockpitDocumentActivityMetricsQueryService = cockpitDocumentActivityMetricsQueryService;
            _securityHelperService = securityHelperService;
        }

        public QueryHandlerResponse Handle(CockpitDocumentActivityMetricsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(CockpitDocumentActivityMetricsQuery query)
        {
            _logger.LogInformation("Entered {HandlerName} with query: {Query}  ",
                nameof(CockpitDocumentActivityMetricsQuery), JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                if (query.IsUserLevel is false && !HaveSufficientPermissionToReadDepartmentLevelData())
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "You do not have sufficient permission to read department level data.";
                }
                else
                {
                    response.StatusCode = 0;
                    response.Results = await _cockpitDocumentActivityMetricsQueryService.InitiateGetCockpitDocumentActivityMetrics(query);
                }
                
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;

                _logger.LogError("Exception in the query handler {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(CockpitDocumentActivityMetricsQueryHandler), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by {HandlerName} with query: {Query}  ",
                nameof(CockpitDocumentActivityMetricsQuery), JsonConvert.SerializeObject(query));
            return response;
        }

        private bool HaveSufficientPermissionToReadDepartmentLevelData()
        {
            return _securityHelperService.IsAPowerUser() || _securityHelperService.IsAAdminBUser() ||
                   _securityHelperService.IsAAdminOrTaskConrtroller();
        }
    }
}
