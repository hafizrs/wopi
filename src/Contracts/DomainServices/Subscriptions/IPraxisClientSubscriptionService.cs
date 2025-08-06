using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Subscriptions.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisClientSubscriptionService
    {
        Task<PraxisClientSubscription> GetSubscriptionDataByPaymentDetailId(string paymentDetailId);
        Task<PraxisClientSubscription> GetOrganizationLatestSubscriptionData(string organizationId);
        Task<PraxisClientSubscription> GetClientLatestSubscriptionData(string clientId); 
        Task SaveSubscriptionRelatedDataOnPurchase(
            PraxisOrganization organizationData,
            string paymentDetailId,
            string adminEmail,
            string deputyAdminEmail,
            ClientBillingAddress billingAddress,
            ResponsiblePerson responsiblePerson);
         
        Task<bool> SaveSubscriptionNotification(string organizationId, PraxisClientSubscription subscriptionData, bool isAPurchase = true);
        Task<bool> SaveSubscriptionNotificationForClient(string clientId, PraxisClientSubscription subscriptionData, bool isAPurchase = true);

        DateTime GetSubcriptionStartDateTime(DateTime subscriptionStartDate);
        DateTime GetSubcriptionExpiryDateTime(DateTime subscriptionStartDate, int subscriptionDuration);
        DateTime GetSubcriptionRenewalStartDateTime(DateTime subscriptionStartDate, int subscriptionDuration);
        Task SaveClientSubscriptionOnClientCreateUpdate(string clientId);
        Task SaveClientSubscriptionOnOrgCreateUpdate(string orgId, PraxisClientSubscription subs = null, PraxisOrganization orgData = null);
        Task<bool> UpdateExpiredSubscriptionNotificationData(string organizationId, string clientId);
        Task UpdateSubscriptionRenewalData(PraxisClientSubscription subscriptionData);
        Task UpdateExpiredSubscriptionData(string excludeId, string organizationId, string clientId);
        Task<bool> UpdateSubscriptionRenewalNotificationData(string notificationId);
        Task UpdateSubscriptionInvoicePdfFileId(string subscriptionId, string invoicePdfFileId);
        Task MarkAsPaidOfflineInvoice(MarkAsPaidOfflineInvoiceComman command);
    }
}