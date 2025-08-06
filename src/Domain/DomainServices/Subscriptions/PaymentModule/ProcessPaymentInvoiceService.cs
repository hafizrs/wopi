using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
   public class ProcessPaymentInvoiceService: IProcessPaymentInvoiceService
    {
        private readonly IRepository _repositoryService;
        private readonly ILogger<ProcessPaymentInvoiceService> _logger;
        private readonly IInvoiceGeneratorService _pdfGeneratorService;
        public ProcessPaymentInvoiceService(IRepository repositoryService,
            ILogger<ProcessPaymentInvoiceService> logger,
            IInvoiceGeneratorService pdfGeneratorService)
        {
            this._repositoryService = repositoryService;
            this._logger = logger;
            this._pdfGeneratorService = pdfGeneratorService;
        }

        public async Task PrepareInvoiceData(ClientBillingAddress billingAddress, string paymentHistoryId)
        {
            try
            {
                if (!string.IsNullOrEmpty(paymentHistoryId))
                {
                    var subscriptionData = await _repositoryService.GetItemAsync<PraxisClientSubscription>(x => x.PaymentHistoryId == paymentHistoryId);

                    if (subscriptionData == null)
                    {
                        _logger.LogInformation("SubscriptionData not found for payment history ID {PaymentHistoryId}", paymentHistoryId);
                        return;
                    }

                    var notifySubscriptionInfo = new NotifySubscriptionInfo
                    {
                        NotifySubscriptionId = Guid.NewGuid().ToString(),
                        Context = "generate-invoice",
                        ActionName = "generate-invoice"
                    };

                    var invoiceDate = DateTime.UtcNow;
                    var invoiceData = PrepareInvoiceData(billingAddress, subscriptionData, invoiceDate, notifySubscriptionInfo);

                    await _pdfGeneratorService.CreateInvoiceTemplate(invoiceData);

                    _logger.LogInformation("Invoice pdf generator successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Payment invoice generation. Exception Message: {Message}.", ex.Message);
            }
        }
        public PaymentInvoiceData PrepareInvoiceData(
            ClientBillingAddress billingAddress, 
            PraxisClientSubscription subscriptionData, 
            DateTime invoiceDate,
            NotifySubscriptionInfo notifySubscriptionInfo,
            string languageKey = "")
        {
            return new PaymentInvoiceData()
            {
                BillingAddress = billingAddress,
                SubscriptionInfo = new SubscriptionInformation()
                {
                    SubscriptionId = subscriptionData.ItemId,
                    SubscriptionDate = subscriptionData.SubscriptionDate,
                    NumberOfUser = subscriptionData.NumberOfUser,
                    AverageCost = subscriptionData.AverageCost,
                    DurationOfSubscription = subscriptionData.DurationOfSubscription,
                    GrandTotal = subscriptionData.GrandTotal,
                    Location = subscriptionData.Location,
                    OrganizationType = subscriptionData.OrganizationType,
                    PaymentCurrency = subscriptionData.PaymentCurrency,
                    PerUserCost = subscriptionData.PerUserCost,
                    SubscriptionType = subscriptionData.SubscriptionPackage,
                    TaxPercentage = subscriptionData.TaxPercentage,
                    TaxDeduction = subscriptionData.TaxDeduction,
                    OrganizationId = subscriptionData.OrganizationId,
                    ClientId = subscriptionData.ClientId,
                    PaymentInvoiceId = subscriptionData.PaymentInvoiceId,
                    PaymentHistoryId = subscriptionData.PaymentHistoryId,
                    PaidAmount = subscriptionData.PaidAmount,
                    SupportCost = subscriptionData.SupportSubscriptionInfo?.TotalSupportCost,
                    SubscriptionExpirationDate = subscriptionData.SubscriptionExpirationDate,
                    SubscriptionInstallments = subscriptionData.SubscriptionInstallments,
                    StorageSubscription = subscriptionData.StorageSubscription,
                    TokenSubscription = subscriptionData.TokenSubscription,
                    AmountDue = subscriptionData.AmountDue,
                    PaymentMethod = subscriptionData.PaymentMethod,
                    InvoiceMetaData = subscriptionData.InvoiceMetaData,
                    InvoiceDate = invoiceDate
                },
                NotifySubscriptionInfo = notifySubscriptionInfo,
                LanguageKey = languageKey
            };
        }
    }
}
