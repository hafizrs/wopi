namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IProvideLogoLocation
    {
        string GetLocation(string clientId = null);
    }
}
