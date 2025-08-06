using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
   public class SubscriptionPackagePriceDto
    {
        public List<SubscriptionPackagePriceModel> SubscriptionPrices { get; set; }
        public double SubscriptionPrice { get; set; }
        public PraxisSubscriptionPackagePrice SubscriptionPackageInfo { get; set; } = new PraxisSubscriptionPackagePrice();
    }
}
