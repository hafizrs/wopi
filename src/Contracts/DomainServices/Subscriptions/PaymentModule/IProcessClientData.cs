using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IProcessClientData
    {
        Task<(bool, PraxisClient client)> SaveData(PaymentClientInformation clientInformation);
        Task<bool> ProcessClientSubscription(PraxisClient client, double alreadyIncludedStorage, int additionalStorage);
        Task<bool> ProcessPraxisClientSubscriptionNotification(PraxisClient clientData, PraxisClientSubscription existingClientSubscription);
        SubscriptionPackage GetSubscriptionPackageInfo(PraxisClient client);
        Task<bool> ProcessStorageLicensing(string clientId, double alreadyIncludedStorage, int additionalStorage);
    }
}
