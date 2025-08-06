using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetOpenOrganizationQueryHandler : IQueryHandler<GetOpenOrganizationQuery, OpenOrganizationResponse>
    {
        private readonly ILogger<GetOpenOrganizationQueryHandler> _logger;
        private readonly IProcessOpenOrgRole _processOpenOrgRoleService;

        public GetOpenOrganizationQueryHandler(
            ILogger<GetOpenOrganizationQueryHandler> logger,
            IProcessOpenOrgRole processOpenOrgRoleService)
        {
            _logger = logger;
            _processOpenOrgRoleService = processOpenOrgRoleService;
        }
        [Invocable]
        public OpenOrganizationResponse Handle(GetOpenOrganizationQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query} ", nameof(GetOpenOrganizationQueryHandler),
                JsonConvert.SerializeObject(query));

            var response = _processOpenOrgRoleService.ProcessRole(query.ClientId, query.IsOpenOrganization);

            _logger.LogInformation("Handled By {HandlerName} with response: {Response} ",
                nameof(GetOpenOrganizationQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }

        public Task<OpenOrganizationResponse> HandleAsync(GetOpenOrganizationQuery query)
        {
            throw new NotImplementedException();
        }

        
    }
}
