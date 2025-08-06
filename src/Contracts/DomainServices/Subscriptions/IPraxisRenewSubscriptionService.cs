using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisRenewSubscriptionService
    {
        Task<CommandResponse> IsAValidRenewSubscriptionRequestForOrg(string orgId); 
        Task<CommandResponse> IsAValidRenewSubscriptionRequestForClient(string clientId);
        Task<PraxisClientSubscriptionNotification> GetOrganizationCurrentSubscriptionNotificationData(string organizationId);
        Task<PraxisClientSubscriptionNotification> GetClientCurrentSubscriptionNotificationData(string clientId);

    }
}
