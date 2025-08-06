using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SubscriptionRenewalCommand
    {
        public string OrganizationId { get; set; }
        public string SubscriptionId { get; set; }
        public int NumberOfUser { get; set; }
        public double TotalAdditionalTokenInMillion { get; set; }
        public double TotalAdditionalTokenCost { get; set; }
        public double TotalAdditionalManualTokenInMillion { get; set; }
        public double TotalAdditionalManualTokenCost { get; set; }
        public double TotalTokenInMillion { get; set; }
        public double TotalTokenCost { get; set; }
        public double TotalIncludedStorageInGigaBites { get; set; }
        public double TotalAdditionalStorageInGigaBites { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string PaymentMode { get; set; }
        public double NumberOfAuthorizedUsers { get; set; }
        public bool IsTokenApplied { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
        public int PaidDuration { get; set; }
        public List<PraxisKeyValue> TotalPerMonthDueCosts { get; set; } = new List<PraxisKeyValue>();
        public double TaxAmount { get; set; }
        public double AmountToPay { get; set; }
        public double AmountDue { get; set; }
        public bool IsManualTokenApplied { get; set; }
    }
}
