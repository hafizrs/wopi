using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDepartmentSubscriptionQueryHandler : IQueryHandler<GetDepartmentSubscriptionQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDepartmentSubscriptionQueryHandler> _logger;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;

        public GetDepartmentSubscriptionQueryHandler(
            ILogger<GetDepartmentSubscriptionQueryHandler> logger,
            IDepartmentSubscriptionService departmentSubscriptionService)
        {
            _logger = logger;
            _departmentSubscriptionService = departmentSubscriptionService;
        }

        public QueryHandlerResponse Handle(GetDepartmentSubscriptionQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDepartmentSubscriptionQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetDepartmentSubscriptionQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.PraxisClientId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid departmentId";
                }
                else 
                {
                    DepartmentSubscriptionResponse departmentSubscriptionResponses = await _departmentSubscriptionService.GetDepartmentSubscription(query);
                    response.Data = departmentSubscriptionResponses;
                    response.StatusCode = 0;
                    response.TotalCount = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetDepartmentSubscriptionQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetDepartmentSubscriptionQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
