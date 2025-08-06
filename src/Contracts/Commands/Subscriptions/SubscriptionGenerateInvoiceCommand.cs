using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SubscriptionGenerateInvoiceCommand
    {
        public string PaymentHistoryId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string NotifySubscriptionId { get; set; }
        public string Context { get; set; }
        public string ActionName { get; set; }
        public string LanguageKey { get; set; } = "";
    }
}
