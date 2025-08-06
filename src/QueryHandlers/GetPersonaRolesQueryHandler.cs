using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPersonaRolesQueryHandler : IQueryHandler<GetPersonaRolesQuery, PersonaRoleResponse>
    {
        private readonly ILogger<GetPersonaRolesQueryHandler> _logger;
        private readonly IPreparePersonaRoleMap _preparePersonaRoleMapService;

        public GetPersonaRolesQueryHandler(
            ILogger<GetPersonaRolesQueryHandler> logger,
            IPreparePersonaRoleMap preparePersonaRoleMapService
            )
        {
            _logger = logger;
            _preparePersonaRoleMapService = preparePersonaRoleMapService;
        }

        [Invocable]
        public PersonaRoleResponse Handle(GetPersonaRolesQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",nameof(GetPersonaRolesQueryHandler),JsonConvert.SerializeObject(query));
            var response = _preparePersonaRoleMapService.GeneratePersonaRoles(query);
            _logger.LogInformation("Handled By {HandlerName} with query: {Query}.",
                nameof(GetPersonaRolesQueryHandler), JsonConvert.SerializeObject(query));
            return response;
        }

        public Task<PersonaRoleResponse> HandleAsync(GetPersonaRolesQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
