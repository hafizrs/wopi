using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class PricingSeedDataResponse
    {
        public IEnumerable<SubscriptionPackage> SubscriptionPackages { get; set; }
        public IEnumerable<PricingSubscription> Subscriptions { get; set; }
        public List<TaxForCountryModel> TaxForCountry { get; set; }
        public string DefaultCurrency { get; set; }
        public SupportSubscriptionPackage SupportSubscriptionPackage { get; set; }
        public StorageSubscriptionSeed StorageSubscriptionSeed { get; set; }
        public TokenSubscriptionSeed TokenSubscriptionSeed { get; set; }
        public PraxisSubscriptionPackagePrice PackagePrice { get; set; }
    }
}
