using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule
{
    public class SubscriptionUpdateForClientCommand
    {
        public string ClientId { get; set; } 
        public string SubscriptionId { get; set; }
        public double TotalAdditionalTokenInMillion { get; set; } 
        public double TotalAdditionalManualTokenInMillion { get; set; } 
        public double TotalAdditionalTokenCost { get; set; }   
        public double TotalAdditionalManualTokenCost { get; set; }   
        public double TotalAdditionalStorageInGigaBites { get; set; }
        public double TotalAdditionalStorageCost { get; set; } 
        public double TaxDeduction { get; set; }  
        public double GrandTotal { get; set; }
        public int DurationOfSubscription { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
        public string PaymentMode { get; set; } 
        public bool IsTokenApplied { get; set; }
        public bool IsManualTokenApplied { get; set; }
        public bool IsAdditionalAllocation { get; set; } = false;
    }
}
