using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDepartmentWiseSuppliersQueryHandler : IQueryHandler<GetDepartmentWiseSuppliersQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDepartmentWiseSuppliersQueryHandler> _logger;
        private readonly IPraxisClientService _praxisClientService;

        public GetDepartmentWiseSuppliersQueryHandler(
            ILogger<GetDepartmentWiseSuppliersQueryHandler> logger,
            IPraxisClientService praxisClientService)
        {
            _logger = logger;
            _praxisClientService = praxisClientService;
        }

        public QueryHandlerResponse Handle(GetDepartmentWiseSuppliersQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDepartmentWiseSuppliersQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetDepartmentWiseSuppliersQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.OrganizationId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid organizationId";
                }
                else 
                {
                    List<DepartmentWiseSuppliersResponse> supplierResponses = await _praxisClientService.GetDepartmentWiseSuppliers(query);
                    response.Data = supplierResponses;
                    response.StatusCode = 0;
                    response.TotalCount = supplierResponses?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetDepartmentWiseSuppliersQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetDepartmentWiseSuppliersQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
