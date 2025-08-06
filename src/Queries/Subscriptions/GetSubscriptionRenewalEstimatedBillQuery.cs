namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetSubscriptionRenewalEstimatedBillQuery
    {
        public string OrganizationId { get; set; }
        public string ClientId { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionTypeSeedId { get; set; }
        public int NumberOfUser { get; set; }
        public int DurationOfSubscription { get; set; }
        public int NumberOfSupportUnit { get; set; }
        public double TotalAdditionalStorageInGigaBites { get; set; }
        public double TotalAdditionalTokenInMillion { get; set; }
        public int PaidDuration { get; set; }
        public double TotalAdditionalManualTokenInMillion { get; set; }
    }
}
