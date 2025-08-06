namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation
{
    public interface INavigationPreparationTypeStrategy
    {
        IDynamicNavigationPreparation GetServiceType(string type);
    }
}
