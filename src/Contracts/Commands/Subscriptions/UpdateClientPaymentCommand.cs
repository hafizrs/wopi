using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateClientPaymentCommand
    {
        public string OrganizationId { get; set; }
        public string SubscriptionId { get; set; }
        public string PaymentCurrency { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
        public string NotificationSubscriptionId { get; set; } 
        public double AmountToPay { get; set; }
        public int PaidDuration { get; set; }
        public double? AmountDue { get; set; }
    }
}
