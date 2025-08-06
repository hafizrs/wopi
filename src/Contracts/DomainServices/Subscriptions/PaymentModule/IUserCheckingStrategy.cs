namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IUserCheckingStrategy
    {
        IProcessUserInformation GetServiceType(bool isExist, string context);
    }
}
