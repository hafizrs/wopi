using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class SubscriptionPricingCustomPackageResponse
    {
        public string ItemId { get; set; }
        public int NumberOfUser { get; set; }
        public double? DiscountOnPerUserAmount { get; set; }
        public double? DiscountAmount { get; set; }
        public double? DiscountPercentage { get; set; }
        public DateTime ValidityDate { get; set; }
        public bool IsSubscriptionUsed { get; set; }
    }
}
