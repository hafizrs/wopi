using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class RemoveCustomSubscriptionCommand
    {
        public string ClientId { get; set; }
        public int NumberOfUser { get; set; }
        public int DurationOfSubscription { get; set; }
        public int AdditionalStorage { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime SubscriptionDate { get; set; }
    }
}
