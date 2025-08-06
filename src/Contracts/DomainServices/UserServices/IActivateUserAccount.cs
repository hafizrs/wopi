using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IActivateUserAccount
    {
        Task ActivateAccount(string clientId);
    }
}
