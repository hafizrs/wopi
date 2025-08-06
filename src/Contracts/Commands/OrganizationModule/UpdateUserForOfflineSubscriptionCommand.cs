using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateUserForOfflineSubscriptionCommand
    {
        public PraxisClientSubscription SubscriptionData { get; set; }
        public string OrganizationId { get; set; }
    }
}