namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
   public class SubscriptionPlan
    {
       public int? NumberOfUser { get; set; }
       public string SubscriptionPackage { get; set; }
       public int? DurationOfSubscription { get; set; }
    }
}
