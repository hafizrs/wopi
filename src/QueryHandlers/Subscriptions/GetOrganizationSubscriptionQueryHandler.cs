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
    public class GetOrganizationSubscriptionQueryHandler : IQueryHandler<GetOrganizationSubscriptionQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetOrganizationSubscriptionQueryHandler> _logger;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;

        public GetOrganizationSubscriptionQueryHandler(
            ILogger<GetOrganizationSubscriptionQueryHandler> logger,
            IOrganizationSubscriptionService organizationSubscriptionService)
        {
            _logger = logger;
            _organizationSubscriptionService = organizationSubscriptionService;
        }

        public QueryHandlerResponse Handle(GetOrganizationSubscriptionQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetOrganizationSubscriptionQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetOrganizationSubscriptionQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.OrganizationId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid departmentId";
                }
                else 
                {
                    OrganizationSubscriptionResponse organizationSubscriptionResponses = await _organizationSubscriptionService.GetOrganizationSubscription(query);
                    response.Data = organizationSubscriptionResponses;
                    response.StatusCode = 0;
                    response.TotalCount = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(GetOrganizationSubscriptionQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetOrganizationSubscriptionQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
