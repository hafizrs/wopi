using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
   public interface ISubscriptionCalculationService
    {
        PraxisClientSubscription GetCurrentSubscriptionData(string organizationId, string subscriptionId);
        Task<PraxisPaymentModuleSeed> GetSubscriptionSeedData(string subscriptionTypeSeedId);
        List<CalculatedPriceModel> CalculateSubscriptionPrice(int numberOfUser, string subscriptionTypeSeedId, int durationOfSubscription, double? SubscriptionPrice = 0);
        List<CalculatedPriceModel> CalculateSubscriptionUpdatePrice(int numberOfUser, string subscriptionTypeSeedId, int durationOfSubscription, string clientId);
        PraxisPaymentModuleSeed GetPricingSubscriptionSeedData(string subscriptionTypeSeedId);
        SubscriptionEstimatedBillResponse CalculateOtherPropertiesOfBillCosts(PraxisPaymentModuleSeed praxisPaymentModuleSeed, SubscriptionEstimatedBillResponse billResponse, string subscriptionId, string organizationId, int durationOfSubscription);
        List<CalculatedPriceModel> GetCompletePackageSubscriptionUpdatePrice(
           string organizationId,
           string subscriptionTypeSeedId,
           int numberOfUser,
           int durationOfSubscription);

        List<CalculatedPriceModel> GetCompletePackageSubscriptionUpdatePriceForClient(
          string clientId,
          string subscriptionTypeSeedId,
          int numberOfUser,
          int durationOfSubscription);

        string GetSubscriptionPaymentMethod(int duration);
        List<(SubscriptionPeriod period, double price)> CalculatePeriodPrices(int numberOfUser, string subscriptionPackage);
    }
}
