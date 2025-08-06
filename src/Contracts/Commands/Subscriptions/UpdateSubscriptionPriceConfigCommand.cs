using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateSubscriptionPriceConfigCommand
    {
        public double? PerUserAnnualCost { get; set; }
        public double? PerUserSemiAnnualCost { get; set; }
        public double? PerUserQuaterlyCost { get; set; }
        public double? PerMillionTokenCost { get; set; }
        public double? PerGBStorageCost { get; set; }
        public List<TaxForCountryModel>? TaxForCountry { get; set; }
    }
}
