using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class IsOrganizationExistQueryHandler : IQueryHandler<IsOrganizationExistQuery, QueryHandlerResponse>
    {
        private readonly ILogger<IsOrganizationExistQueryHandler> _logger;
        private readonly IPraxisOrganizationExistCheckService _ipraxisOrganizationExistCheckService;
        public IsOrganizationExistQueryHandler(ILogger<IsOrganizationExistQueryHandler> logger,
            IPraxisOrganizationExistCheckService ipraxisOrganizationExistCheckService)
        {
            _logger = logger;
            _ipraxisOrganizationExistCheckService = ipraxisOrganizationExistCheckService;
        }

        public QueryHandlerResponse Handle(IsOrganizationExistQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(IsOrganizationExistQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(IsOrganizationExistQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                return await _ipraxisOrganizationExistCheckService.CheckOrganizationNameExistance(query.OrganizationName, query.OrganizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(IsOrganizationExistQueryHandler), ex.Message, ex.StackTrace);
                
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }
    }
}
