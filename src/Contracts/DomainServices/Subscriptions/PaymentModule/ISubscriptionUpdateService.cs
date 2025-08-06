using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface ISubscriptionUpdateService
    {
        Task<bool> InitiateSubscriptionUpdatePaymentProcess(SubscriptionUpdateCommand command);
        Task<bool> InitiateSubscriptionUpdatePaymentProcessForClient(SubscriptionUpdateForClientCommand command);
        Task<bool> InitiateSubscriptionUpdateForAllocation(SubscriptionUpdateForClientCommand command);
    }
}
