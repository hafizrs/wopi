using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Microsoft.Extensions.Configuration;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;

using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class SubscriptionRenewalService : ISubscriptionRenewalService
    {
        private readonly ILogger<SubscriptionRenewalService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IServiceClient _serviceClient;
        private readonly INotificationService _notificationProviderService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISubscriptionRenewalEstimatedBillGenerationService _subscriptionRenewalEstimatedBillGenerationService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly ISubscriptionCalculationService _subscriptionCalculationService;

        private readonly string _paymentServiceBaseUrl;
        private readonly string _paymentServiceVersion;
        private readonly string _paymentServiceInitializeUrl;

        private readonly string _praxisWebUrl;
        private readonly string _paymentFailUrl;

        public SubscriptionRenewalService(
            ILogger<SubscriptionRenewalService> logger,
            IRepository repository,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider,
            IServiceClient serviceClient,
            INotificationService notificationProviderService,
            ICommonUtilService commonUtilService,
            ISubscriptionRenewalEstimatedBillGenerationService subscriptionRenewalEstimatedBillGenerationService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            ISubscriptionCalculationService subscriptionCalculationService)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _serviceClient = serviceClient;
            _notificationProviderService = notificationProviderService;
            _commonUtilService = commonUtilService;
            _subscriptionRenewalEstimatedBillGenerationService = subscriptionRenewalEstimatedBillGenerationService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _paymentServiceBaseUrl = configuration["PaymentServiceBaseUrl"];
            _paymentServiceVersion = configuration["PaymentServiceVersion"];
            _paymentServiceInitializeUrl = configuration["PaymentServiceInitializeUrl"];
            _paymentFailUrl = configuration["PaymentFailUrl"];
            _praxisWebUrl = configuration["PraxisWebUrl"];
            _subscriptionCalculationService = subscriptionCalculationService;
        }

        public async Task<bool> InitiateSubscriptionRenewalPaymentProcess(SubscriptionRenewalCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionRenewalService));

            try
            {
                var praxisPaymentModuleSeed = PraxisPaymentModuleSeed();
                var subscriptionData = _repository.GetItem<PraxisClientSubscription>(s => s.ItemId == command.SubscriptionId);
                var additionalToken = (double)(subscriptionData?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
                var additionalManualToken = (double)(subscriptionData?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

                var estimatedBill = await _subscriptionRenewalEstimatedBillGenerationService.GenerateSubscriptionRenewalEstimatedBill(
                     command.OrganizationId, null, command.SubscriptionId,
                     PraxisPriceSeed.PraxisPaymentModuleSeedId,
                     command.NumberOfUser, 12, 0, 0, additionalToken, additionalManualToken);

                var paymentModel = GetSubscriptionPaymentModelForOrg(command, estimatedBill, praxisPaymentModuleSeed);

                var paymentProcessResponse = await GetPaymentRedirectionUrl(command, paymentModel);

                if (paymentProcessResponse.StatusCode == 0)
                {
                    var previousSubscription = GetPraxisClientSubscription(command.OrganizationId);

                    await SaveLatestSubscriptionData(
                        command,
                        praxisPaymentModuleSeed,
                        paymentModel,
                        previousSubscription,
                        paymentProcessResponse.PaymentDetailId);

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        Url = paymentProcessResponse.RedirectUrl
                    });

                    await SendPaymnetNotification(command, true, denormalizePayload);
                }
                else
                {
                    _logger.LogError("Something went wrong when making payment. Error message: {ErrorMessage} and status code: {StatusCode}.", paymentProcessResponse.ErrorMessage, paymentProcessResponse.StatusCode);

                    await SendPaymnetNotification(command, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionRenewalService), ex.Message, ex.StackTrace);

                await SendPaymnetNotification(command, false);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionRenewalService));

            return true;
        }

        public async Task<bool> InitiateSubscriptionRenewalPaymentProcessForClient(SubscriptionRenewalForClientCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionRenewalService));

            try
            {
                var praxisPaymentModuleSeed = PraxisPaymentModuleSeed();
                var subscriptionData = _repository.GetItem<PraxisClientSubscription>(s => s.ItemId == command.SubscriptionId);

                var additionalToken = (double)(subscriptionData?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
                var additionalManualToken = (double)(subscriptionData?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

                var estimatedBill = await _subscriptionRenewalEstimatedBillGenerationService.GenerateSubscriptionRenewalEstimatedBill(
                    null, 
                    command.ClientId, 
                    command.SubscriptionId,
                    PraxisPriceSeed.PraxisPaymentModuleSeedId,
                    0, 
                    12, 
                    0, 
                    command.TotalAdditionalStorageInGigaBites, 
                    additionalToken,
                    additionalManualToken);

                var paymentModel = GetSubscriptionPaymentModel(estimatedBill, praxisPaymentModuleSeed, command.PaidDuration);

                var paymentProcessResponse = await GetPaymentRedirectionUrlForClient(command, paymentModel);

                if (paymentProcessResponse?.StatusCode == 0)
                {
                    var previousSubscription = GetPraxisClientSubscriptionForClient(command.ClientId);

                    await SaveLatestSubscriptionDataForClient(
                        command,
                        praxisPaymentModuleSeed,
                        paymentModel,
                        previousSubscription,
                        paymentProcessResponse?.PaymentDetailId);

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        Url = paymentProcessResponse?.RedirectUrl
                    });

                    await SendPaymnetNotificationForClient(command, true, denormalizePayload);
                }
                else
                {
                    _logger.LogError("Something went wrong when making payment. Error message: {ErrorMessage}, Status code: {StatusCode}.",
                    paymentProcessResponse.ErrorMessage,
                    paymentProcessResponse.StatusCode);

                    await SendPaymnetNotificationForClient(command, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in the service {ServiceName}. Exception Message: {ExceptionMessage}", 
                    nameof(SubscriptionRenewalService), 
                    ex.Message);

                await SendPaymnetNotificationForClient(command, false);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionRenewalService));

            return true;
        }

        private PraxisPaymentModuleSeed PraxisPaymentModuleSeed()
        {
            return _repository.GetItem<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
        }

        private SubscriptionPaymentModel GetSubscriptionPaymentModel(
            SubscriptionEstimatedBillResponse estimatedBill,
            PraxisPaymentModuleSeed praxisPaymentModuleSeed,
            int paidDuration)
        {
            var completePackageBill = estimatedBill.PackageCosts.FirstOrDefault(c => c.SubscriptionPackage == "COMPLETE_PACKAGE");

            return new SubscriptionPaymentModel
            {
                Currency = praxisPaymentModuleSeed.DefaultCurrency,
                GrandTotal = estimatedBill.GrandTotal,
                PerUserCost = completePackageBill != null ? completePackageBill.PerUserMonthlyPrice : 0.00,
                AverageCost = completePackageBill != null ? completePackageBill.TotalUserMonthlyPrice : 0.00,
                AdditionalStorageCost = estimatedBill.AdditionalStorageCost,
                TaxDeduction = estimatedBill.TaxAmount,
                SubcriptionInstallments = estimatedBill?.InstallmentPaymentResults.FirstOrDefault(x => x.Duration == paidDuration) ?? new CalculatedInstallmentPaymentModel(),
                AmountDue = estimatedBill.AmountDue
            };
        }

        private SubscriptionPaymentModel GetSubscriptionPaymentModelForOrg(
           SubscriptionRenewalCommand command,
           SubscriptionEstimatedBillResponse estimatedBill,
           PraxisPaymentModuleSeed praxisPaymentModuleSeed)
        {
            var completePackageBill = estimatedBill.PackageCosts.FirstOrDefault(c => c.SubscriptionPackage == "COMPLETE_PACKAGE");

            return new SubscriptionPaymentModel
            {
                Currency = praxisPaymentModuleSeed.DefaultCurrency,
                GrandTotal = command.AmountToPay,
                PerUserCost = completePackageBill != null ? completePackageBill.PerUserMonthlyPrice : 0.00,
                AverageCost = completePackageBill != null ? completePackageBill.TotalUserMonthlyPrice : 0.00,
                AdditionalStorageCost = estimatedBill.AdditionalStorageCost,
                TaxDeduction = command.TaxAmount,
                AmountDue = command.AmountDue
            };
        }

        private SubscriptionPackage GetCompleteSubcriptionPackageInfo(PraxisPaymentModuleSeed praxisPaymentModuleSeed)
        {
            return praxisPaymentModuleSeed.SubscriptionPackages.FirstOrDefault(x => x.ItemId == PraxisPriceSeed.CompletePackageId);
        }

        private async Task<PaymentProcessingResult> GetPaymentRedirectionUrl(
            SubscriptionRenewalCommand command,
            SubscriptionPaymentModel paymentModel)
        {
            var paymentMicroserviceModel = new PaymentMicroserviceModel()
            {
                ProviderName = "SIX",
                Amount = paymentModel.GrandTotal,
                CurrencyCode = paymentModel.Currency,
                OrderId = Guid.NewGuid().ToString(),
                Description = "Order Description",
                NotificationUrl = _praxisWebUrl + "/api/business-praxismonitor/PraxisMonitorWebService/PraxisMonitorQuery/ValidateUpdatePayment",
                SuccessUrl = $"{_praxisWebUrl}/organization/{command.OrganizationId}/billing-details",
                FailUrl = $"{_praxisWebUrl}/{_paymentFailUrl}"
            };

            var token = await GetAdminToken();

            var respnose = await _serviceClient.SendToHttpAsync<PaymentProcessingResult>(
                HttpMethod.Post,
                _paymentServiceBaseUrl,
                _paymentServiceVersion,
                _paymentServiceInitializeUrl,
                paymentMicroserviceModel,
                token);

            if (respnose.StatusCode != 0)
            {
                _logger.LogError("Error occurred during initiate payment by payment service. Error: {Response} and exception -> {ErrorMessage}", JsonConvert.SerializeObject(respnose), respnose.ErrorMessage);
            }
            return respnose;
        }

        private async Task<PaymentProcessingResult> GetPaymentRedirectionUrlForClient(
            SubscriptionRenewalForClientCommand command,
            SubscriptionPaymentModel paymentModel)
        {
            var paymentMicroserviceModel = new PaymentMicroserviceModel()
            {
                ProviderName = "SIX",
                Amount = paymentModel.GrandTotal,
                CurrencyCode = paymentModel.Currency,
                OrderId = Guid.NewGuid().ToString(),
                Description = "Order Description",
                NotificationUrl = _praxisWebUrl + "/api/business-praxismonitor/PraxisMonitorWebService/PraxisMonitorQuery/ValidateUpdatePayment",
                SuccessUrl = $"{_praxisWebUrl}/clients/{command.ClientId}/billing-details",
                FailUrl = $"{_praxisWebUrl}{_paymentFailUrl}"
            };

            var token = await GetAdminToken();

            var respnose = await _serviceClient.SendToHttpAsync<PaymentProcessingResult>(
                HttpMethod.Post,
                _paymentServiceBaseUrl,
                _paymentServiceVersion,
                _paymentServiceInitializeUrl,
                paymentMicroserviceModel,
                token);

            if (respnose?.StatusCode != 0)
            {
                _logger.LogError(
                    $"Error occured during initiate payment by payment service." +
                    $" Error: {JsonConvert.SerializeObject(respnose)} and exception -> {respnose?.ErrorMessage}");
            }
            return respnose;
        }

        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "lalu vulu",
                UserName = "laluvulu@yopmail.com",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        private PraxisClientSubscription GetPraxisClientSubscription(string organizationId)
        {
            return _repository.GetItem<PraxisClientSubscription>(s => s.OrganizationId == organizationId && s.IsLatest && s.IsActive);
        }

        private PraxisClientSubscription GetPraxisClientSubscriptionForClient(string clientId) 
        {
            return _repository.GetItem<PraxisClientSubscription>(s => s.ClientId == clientId && s.IsLatest && s.IsActive);
        }

        private async Task<bool> SaveLatestSubscriptionData(
            SubscriptionRenewalCommand command,
            PraxisPaymentModuleSeed praxisPaymentModuleSeed,
            SubscriptionPaymentModel paymentModel,
            PraxisClientSubscription previousSubscription,
            string paymentHistoryId)
        {
            try
            {
                var subscriptionPackageInfo = GetCompleteSubcriptionPackageInfo(praxisPaymentModuleSeed);

                DateTime currentTime = DateTime.UtcNow.ToLocalTime();
                var subsStartDate = _praxisClientSubscriptionService.GetSubcriptionRenewalStartDateTime(
                        previousSubscription.SubscriptionDate,
                        previousSubscription.DurationOfSubscription);

                var clientSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = currentTime,
                    CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                    LastUpdateDate = currentTime,
                    IdsAllowedToRead = previousSubscription.IdsAllowedToRead,
                    NumberOfUser = command.NumberOfUser,
                    CreatedUserCount = previousSubscription.CreatedUserCount,
                    DurationOfSubscription = previousSubscription.DurationOfSubscription,
                    OrganizationType = previousSubscription.OrganizationType,
                    SubscriptionPackage = subscriptionPackageInfo.Title,
                    Location = previousSubscription.Location,
                    PerUserCost = paymentModel.PerUserCost,
                    AverageCost = paymentModel.AverageCost,
                    TaxDeduction = paymentModel.TaxDeduction,
                    GrandTotal = paymentModel.GrandTotal,
                    PaymentCurrency = paymentModel.Currency,
                    PaymentHistoryId = paymentHistoryId,
                    OrganizationId = previousSubscription.OrganizationId,
                    OrganizationName = previousSubscription.OrganizationName,
                    OrganizationEmail = previousSubscription.OrganizationEmail,
                    SubscriptionDate = subsStartDate,
                    SubscriptionExpirationDate = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(subsStartDate, previousSubscription.DurationOfSubscription),
                    IsOrgTypeChangeable = true,
                    SubscritionStatus = nameof(PraxisEnums.INITIATED),
                    ModuleList = subscriptionPackageInfo.ModuleList,
                    PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                    PaidAmount = 0,
                    SupportSubscriptionInfo = previousSubscription.SupportSubscriptionInfo,
                    StorageSubscription = PrepareStorageSubscriptionInfo(command, previousSubscription, paymentModel),
                    TokenSubscription = PrepareTokenSubscriptionInfo(command, previousSubscription),
                    ManualTokenSubscription = PrepareManualTokenSubscriptionInfo(command, previousSubscription),
                    TotalTokenSubscription = PrepareTotalTokenSubscriptionInfo(command),
                    PaymentMode = command.PaymentMode,
                    NumberOfAuthorizedUsers = (int)command.NumberOfAuthorizedUsers,
                    IsTokenApplied = command.IsTokenApplied,
                    SubscriptionInstallments = GetSubscriptionInstallments(paymentModel, command.PaidDuration, currentTime),
                    AmountDue = paymentModel.AmountDue,
                    TotalPerMonthDueCosts = command.TotalPerMonthDueCosts,
                    IsManualTokenApplied = command.IsManualTokenApplied,
                    TaxPercentage = previousSubscription.TaxPercentage,
                    PaymentMethod = _subscriptionCalculationService.GetSubscriptionPaymentMethod(command.PaidDuration),
                    InvoiceMetaData = PrepareInvoiceMetaData(command, paymentModel)
                };

                await _repository.SaveAsync(clientSubscription);

                _logger.LogInformation("Data has been successfully inserted to {SubscriptionName}.", nameof(PraxisClientSubscription));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
    "Exception occurred during inserting data to {EntityName} entity. Exception Message: {ExceptionMessage}",
    nameof(PraxisClientSubscription),
    ex.Message);


                return false;
            }
        }

        private List<SubscriptionInstallment> GetSubscriptionInstallments(SubscriptionPaymentModel paymentModel, int paidDuration, DateTime subsStartDate)
        {
            var installments = new List<SubscriptionInstallment>();

            var installment = new SubscriptionInstallment()
            {
                PaidDuration = paidDuration,
                EndOfActivePeriod = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(subsStartDate, paidDuration),
                PaidAmount = paymentModel.GrandTotal
            };

            installments.Add(installment);

            return installments;
        }

        private async Task<bool> SaveLatestSubscriptionDataForClient(
           SubscriptionRenewalForClientCommand command,
           PraxisPaymentModuleSeed praxisPaymentModuleSeed,
           SubscriptionPaymentModel paymentModel,
           PraxisClientSubscription previousSubscription,
           string paymentHistoryId)
        {
            try
            {
                var subscriptionPackageInfo = GetCompleteSubcriptionPackageInfo(praxisPaymentModuleSeed);

                DateTime currentTime = DateTime.UtcNow.ToLocalTime();
                var subsStartDate = _praxisClientSubscriptionService.GetSubcriptionRenewalStartDateTime(
                        previousSubscription.SubscriptionDate,
                        previousSubscription.DurationOfSubscription);

                var clientSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = currentTime,
                    CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                    LastUpdateDate = currentTime,
                    IdsAllowedToRead = previousSubscription.IdsAllowedToRead,
                    CreatedUserCount = previousSubscription.CreatedUserCount,
                    DurationOfSubscription = previousSubscription.DurationOfSubscription,
                    OrganizationType = previousSubscription.OrganizationType,
                    SubscriptionPackage = subscriptionPackageInfo.Title,
                    Location = previousSubscription.Location,
                    TaxDeduction = paymentModel.TaxDeduction,
                    GrandTotal = paymentModel.GrandTotal,
                    PaymentCurrency = paymentModel.Currency,
                    PaymentHistoryId = paymentHistoryId,
                    ClientId = previousSubscription.ClientId,
                    ClientName = previousSubscription.ClientName,
                    ClientEmail = previousSubscription.ClientEmail,
                    SubscriptionDate = subsStartDate,
                    SubscriptionExpirationDate = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(subsStartDate, command.DurationOfSubscription),
                    SubscritionStatus = nameof(PraxisEnums.INITIATED),
                    ModuleList = subscriptionPackageInfo.ModuleList,
                    PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                    PaidAmount = 0,
                    SupportSubscriptionInfo = previousSubscription.SupportSubscriptionInfo,
                    StorageSubscription = PrepareStorageSubscriptionInfoForClient(command, previousSubscription, paymentModel),
                    TokenSubscription = PrepareTokenSubscriptionInfoForClient(command, previousSubscription, paymentModel),
                    ManualTokenSubscription = PrepareManualTokenSubscriptionInfoForClient(command, previousSubscription, paymentModel),
                    TotalTokenSubscription = PrepareTotalSubscriptionInfoForClient(command, paymentModel),
                    PaymentMode = command.PaymentMode,
                    IsTokenApplied = command.IsTokenApplied,
                    IsManualTokenApplied = command.IsManualTokenApplied,
                    SubscriptionInstallments = GetSubscriptionInstallments(paymentModel, command.PaidDuration, currentTime),
                    AmountDue = paymentModel.AmountDue,
                    TaxPercentage = previousSubscription.TaxPercentage,
                    InvoiceMetaData = PrepareInvoiceMetaDataForClient(command, paymentModel)
                };

                await _repository.SaveAsync(clientSubscription);

                _logger.LogInformation("Data has been successfully inserted to {SubscriptionName}.", nameof(PraxisClientSubscription));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during inserting data to {EntityName} entity. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisClientSubscription), ex.Message, ex.StackTrace);

                return false;
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaData(SubscriptionRenewalCommand command, SubscriptionPaymentModel paymentModel)
        {
            var invoiceMetaData = new List<PraxisKeyValue>();

            string paymentMethod = _subscriptionCalculationService.GetSubscriptionPaymentMethod(command.PaidDuration);

            AddKeyValue("NumberOfUser", (command?.NumberOfUser ?? 0).ToString("F2"));
            AddKeyValue("AdditionalStorage", (command?.TotalAdditionalStorageInGigaBites ?? 0).ToString("F2"));
            AddKeyValue("AdditionalLanguageToken", (command?.TotalAdditionalTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("AdditionalManualToken", (command?.TotalAdditionalManualTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("TaxDeduction", (paymentModel?.TaxDeduction ?? 0).ToString("F2"));
            AddKeyValue("PerUserCost", (paymentModel?.PerUserCost ?? 0).ToString("F2"));
            AddKeyValue("AverageCost", (paymentModel?.AverageCost ?? 0).ToString("F2"));
            AddKeyValue("DurationOfSubscription", paymentModel.DurationOfSubscription.ToString("F2"));
            AddKeyValue("InvoiceType", ((int)SubscriptionInvoiceType.NewOrRenew).ToString());
            AddKeyValue("PaymentMethod", paymentMethod);

            return invoiceMetaData;

            void AddKeyValue(string key, string value)
            {
                invoiceMetaData.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaDataForClient(SubscriptionRenewalForClientCommand command, SubscriptionPaymentModel paymentModel)
        {
            var invoiceMetaData = new List<PraxisKeyValue>();

            string paymentMethod = _subscriptionCalculationService.GetSubscriptionPaymentMethod(command.PaidDuration);

            AddKeyValue("NumberOfUser", "0");
            AddKeyValue("AdditionalStorage", (command?.TotalAdditionalStorageInGigaBites ?? 0).ToString("F2"));
            AddKeyValue("AdditionalLanguageToken", (command?.TotalAdditionalTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("AdditionalManualToken", (command?.TotalAdditionalManualTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("TaxDeduction", (paymentModel?.TaxDeduction ?? 0).ToString("F2"));
            AddKeyValue("PerUserCost", (paymentModel?.PerUserCost ?? 0).ToString("F2"));
            AddKeyValue("AverageCost", (paymentModel?.AverageCost ?? 0).ToString("F2"));
            AddKeyValue("DurationOfSubscription", paymentModel.DurationOfSubscription.ToString("F2"));
            AddKeyValue("InvoiceType", ((int)SubscriptionInvoiceType.NewOrRenew).ToString());
            AddKeyValue("PaymentMethod", paymentMethod);

            return invoiceMetaData;

            void AddKeyValue(string key, string value)
            {
                invoiceMetaData.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

        private StorageSubscriptionInfo PrepareStorageSubscriptionInfo(SubscriptionRenewalCommand command, PraxisClientSubscription previousSubscription, SubscriptionPaymentModel paymentModel)
        {
            var storageSubscriptionInfo = new StorageSubscriptionInfo
            {
                IncludedStorageInGigaBites = previousSubscription?.StorageSubscription?.IncludedStorageInGigaBites ?? 0,
                TotalAdditionalStorageInGigaBites = command.TotalAdditionalStorageInGigaBites,
                TotalAdditionalStorageCost = paymentModel.AdditionalStorageCost
            };

            return storageSubscriptionInfo;
        }

        private StorageSubscriptionInfo PrepareStorageSubscriptionInfoForClient(
            SubscriptionRenewalForClientCommand command, 
            PraxisClientSubscription previousSubscription, 
            SubscriptionPaymentModel paymentModel)
        {
            var storageSubscriptionInfo = new StorageSubscriptionInfo
            {
                IncludedStorageInGigaBites = previousSubscription?.StorageSubscription?.IncludedStorageInGigaBites ?? 0,
                TotalAdditionalStorageInGigaBites = command.TotalAdditionalStorageInGigaBites,
                TotalAdditionalStorageCost = paymentModel.AdditionalStorageCost
            };

            return storageSubscriptionInfo;
        }

        private TokenSubscriptionInfo PrepareTokenSubscriptionInfo(
            SubscriptionRenewalCommand command,
            PraxisClientSubscription previousSubscription)
        {
            var tokenSubscriptionInfo = new TokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.TokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalTokenInMillion,
                TotalAdditionalTokenCost = command.TotalAdditionalTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private ManualTokenSubscriptionInfo PrepareManualTokenSubscriptionInfo(
           SubscriptionRenewalCommand command,
           PraxisClientSubscription previousSubscription)
        {
            var manualTokenSubscriptionInfo = new ManualTokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalManualTokenInMillion,
                TotalAdditionalTokenCost = command.TotalAdditionalManualTokenCost
            };

            return manualTokenSubscriptionInfo;
        }

        private TotalTokenSubscriptionInfo PrepareTotalTokenSubscriptionInfo(SubscriptionRenewalCommand command)
        {
            var totalTokenSubscriptionInfo = new TotalTokenSubscriptionInfo
            {
                TotalTokenInMillion = command.TotalTokenInMillion,
                TotalTokenCost = command.TotalTokenCost
            };

            return totalTokenSubscriptionInfo;
        }

        private TokenSubscriptionInfo PrepareTokenSubscriptionInfoForClient(
            SubscriptionRenewalForClientCommand command,
            PraxisClientSubscription previousSubscription,
            SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new TokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.TokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalTokenInMillion,
                TotalAdditionalTokenCost = paymentModel.AdditionalTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private ManualTokenSubscriptionInfo PrepareManualTokenSubscriptionInfoForClient(
           SubscriptionRenewalForClientCommand command,
           PraxisClientSubscription previousSubscription,
           SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new ManualTokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalManualTokenInMillion,
                TotalAdditionalTokenCost = paymentModel.AdditionalManualTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private TotalTokenSubscriptionInfo PrepareTotalSubscriptionInfoForClient(
           SubscriptionRenewalForClientCommand command,
           SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new TotalTokenSubscriptionInfo
            {
                TotalTokenInMillion = command.TotalAdditionalTokenInMillion + command.TotalAdditionalManualTokenInMillion,
                TotalTokenCost = paymentModel.AdditionalTokenCost + paymentModel.AdditionalManualTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private async Task SendPaymnetNotification(SubscriptionRenewalCommand command, bool response, string denormalizePyload = null)
        {
            var result = new
            {
                NotifiySubscriptionId = command.NotificationSubscriptionId,
                Success = response,
                command.NotificationSubscriptionId
            };

            await _notificationProviderService.PaymentNotification(
                    response,
                    command.NotificationSubscriptionId,
                    result,
                    command.Context,
                    command.ActionName,
                    denormalizePyload);
        }

        private async Task SendPaymnetNotificationForClient(SubscriptionRenewalForClientCommand command, bool response, string denormalizePyload = null)
        {
            var result = new
            {
                NotifiySubscriptionId = command.NotificationSubscriptionId,
                Success = response,
                command.NotificationSubscriptionId
            };

            await _notificationProviderService.PaymentNotification(
                    response,
                    command.NotificationSubscriptionId,
                    result,
                    command.Context,
                    command.ActionName,
                    denormalizePyload);
        }

        public async Task<bool> InitiateSubscriptionRenewalPaymentProcessAsync(string paymentHistoryId, SubscriptionRenewalForClientCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionRenewalService));

            try
            {
                var praxisPaymentModuleSeed = PraxisPaymentModuleSeed();
                var previousSubscription = GetPraxisClientSubscriptionForClient(command.ClientId);

                var additionalToken = (double)(previousSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
                var additionalManualToken = (double)(previousSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

                var estimatedBill = await _subscriptionRenewalEstimatedBillGenerationService.GenerateSubscriptionRenewalEstimatedBill(
                    null,
                    command.ClientId,
                    previousSubscription.ItemId,
                    PraxisPriceSeed.PraxisPaymentModuleSeedId,
                    0,
                    12,
                    0,
                    command?.TotalAdditionalStorageInGigaBites ?? 0,
                    additionalToken,
                    additionalManualToken);

                var paymentModel = GetSubscriptionPaymentModel(estimatedBill, praxisPaymentModuleSeed, 0);

                await SaveLatestSubscriptionDataForClient(
                    command,
                    praxisPaymentModuleSeed,
                    paymentModel,
                    previousSubscription,
                    paymentHistoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionRenewalService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionRenewalService));

            return true;
        }

    }
}