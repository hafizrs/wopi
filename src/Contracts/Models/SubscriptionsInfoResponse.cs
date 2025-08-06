namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SubscriptionsInfoResponse
    {
        public DepartmentSubscriptionResponse DepartmentSubscription { get; set; }  
        public OrganizationSubscriptionResponse OrganizationSubscription { get; set; }
    }
}