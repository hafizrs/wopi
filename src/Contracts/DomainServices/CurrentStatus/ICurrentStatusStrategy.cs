namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus
{
    public interface ICurrentStatusStrategy
    {
        IEntityWiseCurrentStatus GetType(string entityName);
    }
}
