using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona
{
    public class PersonaRoleNameService : IPersonaRoleNameService
    {
        public string GetPersonaRoleName(string role, string organizationId)
        {
            return role.ToLower() switch
            {
                RoleNames.AdminB => $"{RoleNames.AdminB_Dynamic}_{organizationId}",
                RoleNames.Poweruser_Payment => $"{RoleNames.Persona_Poweruser_Payment}_{organizationId}",
                RoleNames.PowerUser => $"{RoleNames.Persona_PowerUser}_{organizationId}",
                RoleNames.Leitung => $"{RoleNames.Persona_Leitung}_{organizationId}",
                RoleNames.MpaGroup1 => $"{RoleNames.Persona_MpaGroup1}_{organizationId}",
                RoleNames.MpaGroup2 => $"{RoleNames.Persona_MpaGroup2}_{organizationId}",
                RoleNames.PowerUser_Leitung => $"{RoleNames.Persona_PowerUser_Leitung}_{organizationId}",
                RoleNames.PowerUser_MpaGroup1 => $"{RoleNames.Persona_PowerUser_MpaGroup1}_{organizationId}",
                RoleNames.PowerUser_MpaGroup2 => $"{RoleNames.Persona_PowerUser_MpaGroup2}_{organizationId}",
                RoleNames.Leitung_MpaGroup1 => $"{RoleNames.Persona_Leitung_MpaGroup1}_{organizationId}",
                RoleNames.Leitung_MpaGroup2 => $"{RoleNames.Persona_Leitung_MpaGroup2}_{organizationId}",
                RoleNames.MpaGroup1_MpaGroup2 => $"{RoleNames.Persona_MpaGroup1_MpaGroup2}_{organizationId}",
                RoleNames.PowerUser_Leitung_MpaGroup1 => $"{RoleNames.Persona_PowerUser_Leitung_MpaGroup1}_{organizationId}",
                RoleNames.PowerUser_Leitung_MpaGroup2 => $"{RoleNames.Persona_PowerUser_Leitung_MpaGroup2}_{organizationId}",
                RoleNames.PowerUser_MpaGroup1_MpaGroup2 => $"{RoleNames.Persona_PowerUser_MpaGroup1_MpaGroup2}_{organizationId}",
                RoleNames.Leitung_MpaGroup1_MpaGroup2 => $"{RoleNames.Persona_Leitung_MpaGroup1_MpaGroup2}_{organizationId}",
                RoleNames.PowerUser_Leitung_MpaGroup1_MpaGroup2 => $"{RoleNames.All_Roles}_{organizationId}",
                _ => string.Empty,
            };
        }
    }
}
