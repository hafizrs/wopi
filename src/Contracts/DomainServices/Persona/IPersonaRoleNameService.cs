namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface IPersonaRoleNameService
    {
        string GetPersonaRoleName(string role, string organizationId);
    }
}
