namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SubscriptionUpdateCommand
    {
        public string OrganizationId { get; set; }
        public string SubscriptionId { get; set; }
        public int NumberOfUser { get; set; }
        public double TotalAdditionalTokenInMillion { get; set; }
        public double TotalAdditionalTokenCost { get; set; }
        public double TotalAdditionalManualTokenInMillion { get; set; }
        public double TotalAdditionalManualTokenCost { get; set; }
        public double TotalAdditionalStorageInGigaBites { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string PaymentMode { get; set; }
        public double NumberOfAuthorizedUsers { get; set; }
        public bool IsTokenApplied { get; set; }
        public bool IsManualTokenApplied { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
    }
}
