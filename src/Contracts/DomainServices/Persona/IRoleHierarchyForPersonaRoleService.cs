namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona
{
    public interface IRoleHierarchyForPersonaRoleService
    {
        string[] GetParentList(string role);
    }
}
