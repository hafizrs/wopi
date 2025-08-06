using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class SubscriptionPriceConfigResponse
    {
        public double PerUserAnnualCost { get; set; }
        public double PerUserSemiAnnualCost { get; set; }
        public double PerUserQuaterlyCost { get; set; }
        public double PerMillionTokenCost { get; set; }
        public double PerGBStorageCost { get; set; }
        public List<TaxForCountryModel> TaxForCountry { get; set; }
    }
}
