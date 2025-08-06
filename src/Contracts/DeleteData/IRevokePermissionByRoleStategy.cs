namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IRevokePermissionByRoleStrategy
    {
        IRevokePermissionForRoleSpecific GetService(string role);
    }
}
