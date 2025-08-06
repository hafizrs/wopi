using Selise.Ecap.Entities.PrimaryEntities.Security;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface ISaveDataToPersonaRoleMap
    {
        (bool, string) SaveDataToPersonaRoleMapTable(
            string role,
            string personaName,
            bool IsPowerUser,
            bool isPaymentPowerUser,
            string departmentId,
            string organizationId);
        bool SavePersonaRoleMap(string personaName, PersonaRole[] personaRoles);
    }
}
