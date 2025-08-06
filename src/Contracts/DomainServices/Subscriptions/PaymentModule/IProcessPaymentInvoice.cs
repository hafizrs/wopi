using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IProcessPaymentInvoiceService
    {
        Task PrepareInvoiceData(ClientBillingAddress billingAddress, string paymentHistoryId);
        PaymentInvoiceData PrepareInvoiceData(ClientBillingAddress billingAddress, PraxisClientSubscription subscriptionData, DateTime invoiceDate, NotifySubscriptionInfo notifySubscriptionInfo, string languageKey = "");
    }
}
