using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface ISubscriptionUpdateEstimatedBillGenerationService
    {
        Task<SubscriptionEstimatedBillResponse> GenerateSubscriptionUpdateEstimatedBill(
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
