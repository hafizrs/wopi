
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
   public class PaymentInvoiceData
    {
       public ClientBillingAddress BillingAddress { get; set; }
       public SubscriptionInformation SubscriptionInfo { get; set; }
       public NotifySubscriptionInfo NotifySubscriptionInfo { get; set; }
       public string LanguageKey { get; set; } = string.Empty;

    }
    public class SubscriptionInformation
    {
        public DateTime SubscriptionDate { get; set; }
        public int NumberOfUser { get; set; }
        public int DurationOfSubscription { get; set; }
        public string OrganizationType { get; set; }
        public string SubscriptionType { get; set; }
        public string Location { get; set; }
        public double PerUserCost { get; set; }
        public double AverageCost { get; set; }
        public double TaxDeduction { get; set; }
        public double GrandTotal { get; set; }
        public string PaymentCurrency { get; set; }
        public string OrganizationId { get; set; }
        public string ClientId { get; set; }
        public string SubscriptionId { get; set; }
        public string PaymentInvoiceId { get; set; }
        public string PaymentHistoryId { get; set; }
        public bool FromPurchase { get; set; }
        public double? PaidAmount { get; set; }
        public double? SupportCost { get; set; }
        public DateTime SubscriptionExpirationDate { get; set; }
        public List<SubscriptionInstallment> SubscriptionInstallments { get; set; }
        public StorageSubscriptionInfo StorageSubscription { get; set; }
        public TokenSubscriptionInfo TokenSubscription { get; set; }
        public double? TaxPercentage { get; set; }
        public double? AmountDue { get; set; }
        public string PaymentMethod { get; set; }
        public List<PraxisKeyValue> InvoiceMetaData { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    }

    public class NotifySubscriptionInfo
    {
        public string NotifySubscriptionId { get; set; }
        public string Context { get; set; }
        public string ActionName { get; set; }
    }
}
