using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDepartmentWiseUserAdditionalInfosQueryHandler : IQueryHandler<GetDepartmentWiseUserAdditionalInfosQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDepartmentWiseUserAdditionalInfosQueryHandler> _logger; 
        private readonly IPraxisClientService _praxisClientService;

        public GetDepartmentWiseUserAdditionalInfosQueryHandler(
            ILogger<GetDepartmentWiseUserAdditionalInfosQueryHandler> logger,
            IPraxisClientService praxisClientService
        )
        {
            _logger = logger;
            _praxisClientService = praxisClientService;
        }

        public QueryHandlerResponse Handle(GetDepartmentWiseUserAdditionalInfosQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDepartmentWiseUserAdditionalInfosQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
                nameof(GetDepartmentWiseUserAdditionalInfosQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.OrganizationId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid organizationId";
                }
                else 
                {
                    List<DepartmentWiseUserAdditionalInfosResponse> categoryResponses = await _praxisClientService.GetDepartmentWiseUserAdditionalInfos(query);
                    response.Data = categoryResponses;
                    response.StatusCode = 0;
                    response.TotalCount = categoryResponses?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetDepartmentWiseUserAdditionalInfosQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetDepartmentWiseUserAdditionalInfosQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
