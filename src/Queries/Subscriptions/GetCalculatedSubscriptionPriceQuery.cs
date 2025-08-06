namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
   public class GetCalculatedSubscriptionPriceQuery
    {
        public int NumberOfUser { get; set; }
        public string SubscriptionTypeSeedId { get; set; }
        public int DurationOfSubscription { get; set; }
        public int? NumberOfSupportUnit { get; set; }
        public double? TotalAdditionalStorageInGigaBites { get; set; }
        public double? TotalAdditionalTokenInMillion { get; set; }
        public double? TotalAdditionalMaulaTokenInMillion { get; set; }
        public string CountryCode { get; set; }
        public double? SubscriptionPrice { get; set; } = 0;
        public double? Discount { get; set; } = 0;
        public double? DiscountPercentage { get; set; } = 0;
        public int PaidDuration { get; set; } = 12;
    }
}
