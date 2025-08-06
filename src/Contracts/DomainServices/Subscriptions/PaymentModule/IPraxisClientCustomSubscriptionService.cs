using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IPraxisClientCustomSubscriptionService
    {
        bool SaveSubscriptionData(PraxisClient client, int numberOfUser, int durationOfSubscription, string paymentMethod, int additionalStorage);
    }
}
