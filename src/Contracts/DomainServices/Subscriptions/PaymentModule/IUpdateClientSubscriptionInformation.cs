using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUpdateClientSubscriptionInformation
    {
        Task ProcessUpdateRenewSubscriptionAfterEffectsForOrg(UpdateClientSubscriptionInformationCommand command);
        Task ProcessUpdateRenewSubscriptionAfterEffectsForClient(UpdateClientSubscriptionInformationCommand command); 
        Task UpdateSubscriptionInformation(UpdateClientSubscriptionInformationCommand command);
        Task<bool> UpdateCustomSubscriptionInformation(UpdateCustomSubscriptionCommand command);
        Task<bool> RemoveCustomSubscriptionInformation(RemoveCustomSubscriptionCommand command);
        Task RenewSubscriptionIfAlreadyExpired(UpdateClientSubscriptionInformationCommand command, PraxisClientSubscription currentSubscription, string notificationId = null);
        Task ProcessSubscriptionInfoAsync(PraxisClientSubscription currentSubscriptionData, string organizationId);
        Task ProcessOfflineUpdateSubscriptionAfterEffects(UpdateClientSubscriptionInformationCommand command);
    }
}
