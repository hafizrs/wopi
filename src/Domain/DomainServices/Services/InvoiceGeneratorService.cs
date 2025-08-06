using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Subscriptions
{
    public class InvoiceGeneratorService : IInvoiceGeneratorService
    {
        private readonly IRepository _repository;
        private readonly ILogger<InvoiceGeneratorService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IHtmlFromTemplateGeneratorService _htmlFromTemplateGeneratorService;
        private readonly ISubscriptionCalculationService _subscriptionEstimatedBillCalculationService;
        private readonly INotificationService _notificationService;

        public InvoiceGeneratorService(
            ILogger<InvoiceGeneratorService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IUilmResourceKeyService uilmResourceKeyService,
            IHtmlFromTemplateGeneratorService htmlFromTemplateGeneratorService,
            ISubscriptionCalculationService subscriptionEstimatedBillCalculationService,
            INotificationService notificationService)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _uilmResourceKeyService = uilmResourceKeyService;
            _htmlFromTemplateGeneratorService = htmlFromTemplateGeneratorService;
            _subscriptionEstimatedBillCalculationService = subscriptionEstimatedBillCalculationService;
            _notificationService = notificationService;
        }

        public async Task CreateInvoiceTemplate(PaymentInvoiceData paymentInvoiceData)
        {
            string templateFileId = PraxisConstants.SubscriptionInvoiceTemplateId;
            var templateEnginePayload = await PrepareTemplateEnginePayload(paymentInvoiceData, templateFileId);
            await _htmlFromTemplateGeneratorService.GenerateHtml(templateEnginePayload);
        }

        private async Task<TemplateEnginePayload> PrepareTemplateEnginePayload(PaymentInvoiceData paymentInvoiceData, string templateFileId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var languageKey = !string.IsNullOrEmpty(paymentInvoiceData.LanguageKey) ? paymentInvoiceData.LanguageKey : securityContext.Language;
            var _translatedStringsAsDictionary = _uilmResourceKeyService
                .GetResourceValueByKeyName(ReportConstants.SubscriptionPaymentInvoiceTranslationsKeys, languageKey);

            var translationMetaDataList = new List<Dictionary<string, object>>();

            var translationMetaData = _translatedStringsAsDictionary?.ToDictionary<KeyValuePair<string, string>, string, object>(translation => translation.Key, translation => translation.Value);

            List<MetaData> metaDataList = new List<MetaData>();
            List<Dictionary<string, object>> invoiceMetaDataList = new List<Dictionary<string, object>>();

            var (name, addressTo) = await GetRecipientDetails(paymentInvoiceData.SubscriptionInfo);
            var praxisPaymentModuleSeed = await _subscriptionEstimatedBillCalculationService.GetSubscriptionSeedData(PraxisPriceSeed.PraxisPaymentModuleSeedId);

            var invoiceType = GetIntValueFromMetaData("InvoiceType", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var paymentMethod = GetStringValueFromMetaData("PaymentMethod", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var durationOfSubscription = GetDoubleValueFromMetaData("DurationOfSubscription", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var numberOfUser = GetDoubleValueFromMetaData("NumberOfUser", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var perUserCost = GetDoubleValueFromMetaData("PerUserCost", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var averageCost = GetDoubleValueFromMetaData("AverageCost", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var additionalStorage = GetDoubleValueFromMetaData("AdditionalStorage", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var additionalLanguageToken = GetDoubleValueFromMetaData("AdditionalLanguageToken", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var additionalManualToken = GetDoubleValueFromMetaData("AdditionalManualToken", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var taxDeduction = GetDoubleValueFromMetaData("TaxDeduction", paymentInvoiceData?.SubscriptionInfo?.InvoiceMetaData ?? new List<PraxisKeyValue>());
            var totalAdditionalTokenInMillion = (additionalLanguageToken + additionalManualToken);
            var totalAdditionalTokenCost = ((additionalLanguageToken + additionalManualToken) * (praxisPaymentModuleSeed?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0));
            var additionalStorageCost = (additionalStorage * (praxisPaymentModuleSeed?.StorageSubscriptionSeed?.PricePerGigaBiteStorage ?? 0));
            var totalUserCost = numberOfUser * perUserCost;
            var totalMonthPrice = (numberOfUser * perUserCost) * durationOfSubscription;
            var subtotal = totalMonthPrice + totalAdditionalTokenCost + additionalStorageCost;
            var subscriptionCost = subtotal;
            var total = subtotal + taxDeduction;
            var taxForCountry = praxisPaymentModuleSeed?.TaxForCountry.Find(t => t.CountryCode.Equals(paymentInvoiceData?.SubscriptionInfo?.Location))?.CountryTax ?? 0;
            var subscriptionExpirationDate = FormatDateWithOrdinalSuffix(paymentInvoiceData.SubscriptionInfo.SubscriptionExpirationDate);
            var subscriptionInstallments = paymentInvoiceData?.SubscriptionInfo?.SubscriptionInstallments?.LastOrDefault() ?? new SubscriptionInstallment();
            string paidPeriod = $"{paymentInvoiceData.SubscriptionInfo.SubscriptionDate.ToShortDateString()} to {paymentInvoiceData.SubscriptionInfo.SubscriptionExpirationDate.ToShortDateString()}";
            if (subscriptionInstallments.PaidDuration > 0 && paymentMethod != "Annually")
            {
                var paidOnDate = subscriptionInstallments.EndOfActivePeriod.AddMonths(-subscriptionInstallments.PaidDuration);
                paidPeriod = $"{paidOnDate.ToShortDateString()} to {subscriptionInstallments.EndOfActivePeriod.ToShortDateString()}";
                paymentMethod = _subscriptionEstimatedBillCalculationService.GetSubscriptionPaymentMethod(subscriptionInstallments.PaidDuration);
            }


            string FormatCurrency(double value)
            {
                if (value <= 0) return "-";

                var swissFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                swissFormat.NumberGroupSeparator = "'";
                swissFormat.NumberDecimalDigits = 2;

                return $"CHF {value.ToString("#,0.00", swissFormat)}";
            }

            string FormatNumber(double? value)
            {
                if (!value.HasValue || value <= 0) return "-";

                var swissFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                swissFormat.NumberGroupSeparator = "'";
                swissFormat.NumberDecimalDigits = 2;

                return value.Value.ToString("#,0.00", swissFormat);
            }

            var invoiceMetaData = new Dictionary<string, object>
            {
                { "InvoiceId", paymentInvoiceData?.SubscriptionInfo?.PaymentInvoiceId ?? "-" },
                { "InvoiceDate", paymentInvoiceData?.SubscriptionInfo?.InvoiceDate.ToShortDateString() },
                { "SubscriptionDate", paymentInvoiceData.SubscriptionInfo.SubscriptionDate.ToShortDateString() },
                { "SubscriptionExpirationDate", subscriptionExpirationDate },
                { "NumberOfUser", numberOfUser > 0 ? numberOfUser : "-" },
                { "PerUserCost", FormatCurrency(perUserCost) },
                { "TotalUserCost", FormatCurrency(totalUserCost) },
                { "DurationOfSubscription", paymentInvoiceData?.SubscriptionInfo?.DurationOfSubscription },
                { "DurationOfSubscription1", durationOfSubscription > 0 ? durationOfSubscription : "-" },
                { "PerMonthPrice", FormatNumber(averageCost) },
                { "TotalMonthPrice", FormatCurrency(totalMonthPrice) },
                { "TotalAdditionalTokenInMillion", FormatNumber(totalAdditionalTokenInMillion) },
                { "TotalAdditionalTokenCost", FormatCurrency(totalAdditionalTokenCost) },
                { "PricePerMillionToken", totalAdditionalTokenInMillion > 0 ? FormatCurrency(praxisPaymentModuleSeed?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0) : "-" },
                { "TotalAdditionalStorageInGigaBites", FormatNumber(additionalStorage) },
                { "TotalAdditionalStorageCost", FormatCurrency(additionalStorageCost) },
                { "PricePerGigaBiteStorage", additionalStorage > 0 ? FormatCurrency(praxisPaymentModuleSeed?.StorageSubscriptionSeed?.PricePerGigaBiteStorage ?? 0) : "-" },
                { "SubTotal", FormatNumber(subtotal) },
                { "TaxPercentage", taxDeduction > 0 ? paymentInvoiceData?.SubscriptionInfo?.TaxPercentage?.ToString() ?? taxForCountry.ToString() : string.Empty },
                { "TaxDeduction", FormatNumber(taxDeduction) },
                { "Total", FormatNumber(total) },
                { "PaidAmount", FormatNumber(paymentInvoiceData?.SubscriptionInfo?.PaidAmount) },
                { "PaidPeriod",
                    (invoiceType == (int)SubscriptionInvoiceType.NewOrRenew ||
                     invoiceType == (int)SubscriptionInvoiceType.DuePayment)
                        ? paidPeriod
                        : "-" },
                 { "AmountDue",
                    (invoiceType == (int)SubscriptionInvoiceType.NewOrRenew ||
                     invoiceType == (int)SubscriptionInvoiceType.DuePayment)
                        ? FormatNumber(paymentInvoiceData?.SubscriptionInfo?.AmountDue ?? 0)
                        : 0 },
                { "InvoiceType", invoiceType },
                 { "PaymentMethod",
                    (invoiceType == (int)SubscriptionInvoiceType.NewOrRenew ||
                     invoiceType == (int)SubscriptionInvoiceType.DuePayment)
                        ? paymentMethod
                        : "-" },
            };

            invoiceMetaDataList.Add(invoiceMetaData);
            translationMetaDataList.Add(translationMetaData);
            var metaData = new MetaData()
            {
                Name = "Subscriptions",
                Values = invoiceMetaDataList
            };
            metaDataList.Add(metaData);
            metaData = new MetaData()
            {
                Name = "Translation",
                Values = translationMetaDataList
            };
            metaDataList.Add(metaData);
            metaData = new MetaData() { Name = "Language", Value = securityContext.Language };
            metaDataList.Add(metaData);
            metaData = new MetaData() { Name = "ClientName", Value = name };
            metaDataList.Add(metaData);
            metaData = new MetaData() { Name = "AddressTo", Value = addressTo };
            metaDataList.Add(metaData);
            return new TemplateEnginePayload
            {
                TemplateFileId = templateFileId,
                FileId = Guid.NewGuid().ToString(),
                FileNameExtension = ".html",
                FilteredSqlQueryDatas = new SqlQuery[] { },
                NotifyOnProcessEnding = false,
                RaiseEventOnProcessEnding = true,
                MetaDataList = metaDataList.ToArray(),
                SubscriptionFilterId = paymentInvoiceData.SubscriptionInfo.SubscriptionId,
                EventReferenceData = new Dictionary<string, string>()
                {
                    { "NotifySubscriptionId", paymentInvoiceData.NotifySubscriptionInfo.NotifySubscriptionId },
                    { "SendNotification", paymentInvoiceData?.BillingAddress != null ? "YES" : "NO" },
                    { "EventReference", "PaymentSuccessful" }
                }
            };
        }

        private async Task<(string name, string addressTo)> GetRecipientDetails(SubscriptionInformation subscriptionData)
        {
            if (!string.IsNullOrEmpty(subscriptionData.ClientId))
            {
                var client = await _repository.GetItemAsync<PraxisClient>(x => x.ItemId == subscriptionData.ClientId);
                return (client?.ClientName ?? string.Empty, client?.Address?.AddressLine1 ?? string.Empty);
            }
            else if (!string.IsNullOrEmpty(subscriptionData.OrganizationId))
            {
                var organization = await _repository.GetItemAsync<PraxisOrganization>(x => x.ItemId == subscriptionData.OrganizationId);
                return (organization?.ClientName ?? string.Empty, organization?.Address?.AddressLine1 ?? string.Empty);
            }

            return (string.Empty, string.Empty);
        }

        private StorageSubscriptionInfo CreateStorageSubscription(SubscriptionInformation subscriptionData, PraxisPaymentModuleSeed seed)
        {
            return new StorageSubscriptionInfo
            {
                IncludedStorageInGigaBites = subscriptionData?.StorageSubscription?.IncludedStorageInGigaBites ?? 0,
                TotalAdditionalStorageInGigaBites = subscriptionData?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0,
                TotalAdditionalStorageCost = subscriptionData?.StorageSubscription?.TotalAdditionalStorageCost ?? 0,
                PricePerGigaBiteStorage = seed?.StorageSubscriptionSeed?.PricePerGigaBiteStorage ?? 0
            };
        }

        private TokenSubscriptionInfo CreateTokenSubscription(SubscriptionInformation subscriptionData, PraxisPaymentModuleSeed seed)
        {
            return new TokenSubscriptionInfo
            {
                IncludedTokenInMillion = subscriptionData?.TokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = subscriptionData?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0,
                TotalAdditionalTokenCost = subscriptionData?.TokenSubscription?.TotalAdditionalTokenCost ?? 0,
                PricePerMillionToken = seed?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0
            };
        }

        private static string FormatDateWithOrdinalSuffix(DateTime date)
        {
            int day = date.Day;

            string suffix = day % 10 == 1 && day != 11 ? "st"
                          : day % 10 == 2 && day != 12 ? "nd"
                          : day % 10 == 3 && day != 13 ? "rd"
                          : "th";

            return $"{day}{suffix} {date.ToString("MMMM, yyyy", CultureInfo.InvariantCulture)}";
        }

        private static string FormatDateWithOrdinalSuffix1(DateTime date)
        {
            int day = date.Day;

            string suffix = day % 10 == 1 && day != 11 ? "st"
                          : day % 10 == 2 && day != 12 ? "nd"
                          : day % 10 == 3 && day != 13 ? "rd"
                          : "th";

            // Format the date as "Month Day<suffix>, Year"
            return $"{date.ToString("MMMM", CultureInfo.InvariantCulture)} {day}{suffix}, {date.Year}";
        }

        private int GetIntValueFromMetaData(string key, List<PraxisKeyValue> metaData, int defaultValue = 0)
        {
            var valueString = metaData.FirstOrDefault(kvp => kvp.Key == key)?.Value;
            return int.TryParse(valueString, out var result) ? result : defaultValue;
        }

        private double GetDoubleValueFromMetaData(string key, List<PraxisKeyValue> metaData, double defaultValue = 0)
        {
            var valueString = metaData.FirstOrDefault(kvp => kvp.Key == key)?.Value;
            return double.TryParse(valueString, out var result) ? result : defaultValue;
        }

        private string GetStringValueFromMetaData(string key, List<PraxisKeyValue> metaData)
        {
            var valueString = metaData.FirstOrDefault(kvp => kvp.Key == key)?.Value;
            return !string.IsNullOrEmpty(valueString) ? valueString : string.Empty;
        }
    }
}