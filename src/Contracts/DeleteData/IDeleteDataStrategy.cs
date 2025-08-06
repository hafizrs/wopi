namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IDeleteDataStrategy
    {
        IDeleteDataByCollectionSpecific GetDeleteType(string entityName);
    }
}
