using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SubscriptionEstimatedBillResponse
    {
        public List<CalculatedPriceModel> PackageCosts { get; set; }
        public double AdditionalStorageCost { get; set; }
        public double AdditionalTokenCost { get; set; }
        public double AdditionalManualTokenCost { get; set; }
        public double SupportSubscriptionCost { get; set; }
        public double TaxAmount { get; set; }
        public double GrandTotal { get; set; }
        public double AmountDue { get; set; } = 0;
        public List<CalculatedInstallmentPaymentModel> InstallmentPaymentResults { get; set; } = new List<CalculatedInstallmentPaymentModel>();
    }
}
