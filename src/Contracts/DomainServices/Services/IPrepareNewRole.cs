namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPrepareNewRole
    {
        string SaveRole(string role, string clientId, string chieldRole, bool isDynamic);
    }
}
