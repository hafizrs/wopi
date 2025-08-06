using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface IPreparePersonaRoleMap
    {
        PersonaRoleResponse GeneratePersonaRoles(GetPersonaRolesQuery query);
        public bool PrepareAdminBOrgPersonaRoleMap(
            string personaName,
            List<string> departmentIds,
            List<string> currentDynamicRoles
        );
        string PrepareDynamicRoleForPersona(string role, string clientId);
    }
}
