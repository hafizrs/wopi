using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface ISubscriptionPricingCustomPackageService
    {
        Task<SubscriptionPricingCustomPackageResponse> GetSubscriptionPricingCustomPackage(string id);
        Task<List<SubscriptionPricingCustomPackageResponse>> GetSubscriptionPricingCustomPackages();
        Task SaveOrUpdateSubscriptionCustomPricingPackage(SubscriptionPricingCustomPackageCommand command);  
        Task DeleteSubscriptionCustomPricingPackage(DeleteSubscriptionPricingCustomPackageCommand command);
        bool UpdateSubscriptionUsageID(string itemId, string subscriptionId);
        Task UpdateSubscriptionUsageStatus(string subscriptionId);
    }
}
