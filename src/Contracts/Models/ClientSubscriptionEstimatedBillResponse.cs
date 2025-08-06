using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ClientSubscriptionEstimatedBillResponse 
    {
        public double AdditionalStorageCost { get; set; }
        public double AdditionalTokenCost { get; set; } = 0;
        public double TaxAmount { get; set; }
        public double GrandTotal { get; set; }
    }
}
