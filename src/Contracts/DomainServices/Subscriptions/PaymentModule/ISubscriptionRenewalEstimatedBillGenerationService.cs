using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface ISubscriptionRenewalEstimatedBillGenerationService
    {
        Task<SubscriptionEstimatedBillResponse> GenerateSubscriptionRenewalEstimatedBill(
            string organizationId,
            string clientId,
            string subscriptionId,
            string subscriptionTypeSeedId,
            int numberOfUser,
            int durationOfSubscription,
            int numberOfSupportUnit,
            double additionalStorage,
            double additionalToken,
            double additionalManualToken);
    }
}
