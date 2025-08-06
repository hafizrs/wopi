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
    public class GetDepartmentWiseCategoriesQueryHandler : IQueryHandler<GetDepartmentWiseCategoriesQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDepartmentWiseCategoriesQueryHandler> _logger;
        private readonly IPraxisClientCategoryService _praxisClientCategoryService;

        public GetDepartmentWiseCategoriesQueryHandler(
            ILogger<GetDepartmentWiseCategoriesQueryHandler> logger,
            IPraxisClientCategoryService praxisClientCategoryService)
        {
            _logger = logger;
            _praxisClientCategoryService = praxisClientCategoryService;
        }

        public QueryHandlerResponse Handle(GetDepartmentWiseCategoriesQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDepartmentWiseCategoriesQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetDepartmentWiseCategoriesQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.OrganizationId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid organizationId";
                }
                else 
                {
                    List<DepartmentWiseCategoriesResponse> categoryResponses = await _praxisClientCategoryService.GetDepartmentWiseCategories(query);
                    response.Data = categoryResponses;
                    response.StatusCode = 0;
                    response.TotalCount = categoryResponses?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetDepartmentWiseCategoriesQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetDepartmentWiseCategoriesQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
