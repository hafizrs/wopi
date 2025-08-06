namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetCalculatedSubscriptionUpdatePriceQuery
    {
        public int NumberOfUser { get; set; }
        public string SubscriptionTypeSeedId { get; set; }
        public int DurationOfSubscription { get; set; }
        public string ClientId { get; set; }
        public int? NumberOfSupportUnit { get; set; }
        public double? TotalAdditionalStorageInGigaBites { get; set; }
    }
}
