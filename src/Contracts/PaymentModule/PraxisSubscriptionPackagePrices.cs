using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
   public class PraxisSubscriptionPackagePrice
    {
        [BsonId]
        public string ItemId { get; set; }
        public string SubscriptionPakage { get; set; }
        public List<SubscriptionPackagePriceModel> SubscriptionPackagePrices { get; set; }
        public double OriginalPrice { get; set; }
        public double SemiAnnuallyPrice { get; set; }
        public double QuarterlyPrice { get; set; }
        public FinancialYearInfo FinancialYearInfo { get; set; }
    }
    public class SubscriptionPackagePriceModel
    {
        public int SubscriptionUserBreakingPoint { get; set; }
        public double DiscountOnOriginalPrice { get; set; }

        public SubscriptionPackagePriceModel(int subscriptionUserBreakingPoint, double discountOnOriginalPrice = 0)
        {
            SubscriptionUserBreakingPoint = subscriptionUserBreakingPoint;
            DiscountOnOriginalPrice = discountOnOriginalPrice;
        }
    }
    public class FinancialYearInfo
    {
        public DayMonthInfo StartDate { get; set; }
        public DayMonthInfo EndDate { get; set; }
    }

    public class DayMonthInfo
    {
        public int Day { get; set; }
        public int Month { get; set; }
    }
}
