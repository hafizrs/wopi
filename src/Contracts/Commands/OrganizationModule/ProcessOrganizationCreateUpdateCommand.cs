using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ProcessOrganizationCreateUpdateCommand
    {
        public PraxisClientSubscription SubscriptionData { get; set; }
        public PraxisOrganization OrganizationData { get; set; }
    }
}