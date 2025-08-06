using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface ISubscriptionRenewalService
    {
        Task<bool> InitiateSubscriptionRenewalPaymentProcess(SubscriptionRenewalCommand command);
        Task<bool> InitiateSubscriptionRenewalPaymentProcessForClient(SubscriptionRenewalForClientCommand command);
        Task<bool> InitiateSubscriptionRenewalPaymentProcessAsync(string paymentHistoryId, SubscriptionRenewalForClientCommand command);
    }
}
