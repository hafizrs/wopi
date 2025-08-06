using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SubscriptionPricingCustomPackageCommand
    {
        public string ItemId { get; set; } 
        public int NumberOfUser { get; set; }
        public double? DiscountOnPerUserAmount { get; set; }
        public double? DiscountAmount { get; set; }
        public double? DiscountPercentage { get; set; }
    }
}
