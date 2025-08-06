using Selise.Ecap.Entities.PrimaryEntities.Security;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface IPersonaRolesService
    {
        PersonaRole[] GetPersonaRolesByUserRole(string role, string personaName, string departmentId, string organizationId);
    }
}
