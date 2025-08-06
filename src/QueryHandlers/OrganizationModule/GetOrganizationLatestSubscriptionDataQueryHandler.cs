using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries.OrganizationModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetOrganizationLatestSubscriptionDataQueryHandler : IQueryHandler<GetOrganizationLatestSubscriptionDataQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetOrganizationLatestSubscriptionDataQueryHandler> _logger;
        private readonly IPraxisClientSubscriptionService _service;

        public GetOrganizationLatestSubscriptionDataQueryHandler(
            ILogger<GetOrganizationLatestSubscriptionDataQueryHandler> logger,
            IPraxisClientSubscriptionService service)
        {
            _logger = logger;
            _service = service;
        }

        public QueryHandlerResponse Handle(GetOrganizationLatestSubscriptionDataQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetOrganizationLatestSubscriptionDataQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}",
                nameof(GetOrganizationLatestSubscriptionDataQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                var organizationSubs = await _service.GetOrganizationLatestSubscriptionData(query.OrganizationId);
                response.Data = organizationSubs;
                response.StatusCode = 0;
                response.TotalCount = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName} Exception Message: {ErrorMessage} Exception Details: {StackTrace}.",
                    nameof(GetOrganizationLatestSubscriptionDataQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetOrganizationLatestSubscriptionDataQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }

    }
}
