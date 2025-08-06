namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IDeleteDataRoleAndEntitySpecificStrategy
    {
        IDeleteDataByRoleSpecific GetDeleteType(string role, string entityName);
    }
}
