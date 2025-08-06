using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class SubscriptionPaymentModel
    {
        public string Currency { get; set; }
        public double GrandTotal { get; set; }
        public double PerUserCost { get; set; }
        public double AverageCost { get; set; }
        public double AdditionalStorageCost { get; set; }
        public double AdditionalTokenCost { get; set; }
        public double AdditionalManualTokenCost { get; set; }
        public double SupportSubscriptionCost { get; set; }
        public double TaxDeduction { get; set; }
        public CalculatedInstallmentPaymentModel SubcriptionInstallments { get; set; } = new CalculatedInstallmentPaymentModel();
        public double AmountDue { get; set; } = 0;
        public int DurationOfSubscription { get; set; }
    }
}
