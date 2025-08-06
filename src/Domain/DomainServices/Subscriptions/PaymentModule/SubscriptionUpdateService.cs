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
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.Giraffe;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.DWT;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class SubscriptionUpdateService : ISubscriptionUpdateService
    {
        private readonly ILogger<SubscriptionUpdateService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IServiceClient _serviceClient;
        private readonly INotificationService _notificationProviderService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISubscriptionUpdateEstimatedBillGenerationService _subscriptionUpdateEstimatedBillGenerationService;
        private readonly ISubscriptionCalculationService _subscriptionCalculationService;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IUpdateClientSubscriptionInformation _updateClientSubscriptionInformationService;

        private readonly string _paymentServiceBaseUrl;
        private readonly string _paymentServiceVersion;
        private readonly string _paymentServiceInitializeUrl;

        private readonly string _praxisWebUrl;
        private readonly string _paymentFailUrl;

        public SubscriptionUpdateService(
            ILogger<SubscriptionUpdateService> logger,
            IRepository repository,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider,
            IServiceClient serviceClient,
            INotificationService notificationProviderService,
            ICommonUtilService commonUtilService,
            ISubscriptionUpdateEstimatedBillGenerationService subscriptionUpdateEstimatedBillGenerationService,
            ISubscriptionCalculationService subscriptionCalculationService,
            IPraxisClientService praxisClientService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            ISecurityHelperService securityHelperService,
            IUpdateClientSubscriptionInformation updateClientSubscriptionInformationService)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _serviceClient = serviceClient;
            _notificationProviderService = notificationProviderService;
            _commonUtilService = commonUtilService;
            _subscriptionUpdateEstimatedBillGenerationService = subscriptionUpdateEstimatedBillGenerationService;
            _paymentServiceBaseUrl = configuration["PaymentServiceBaseUrl"];
            _paymentServiceVersion = configuration["PaymentServiceVersion"];
            _paymentServiceInitializeUrl = configuration["PaymentServiceInitializeUrl"];
            _paymentFailUrl = configuration["PaymentFailUrl"];
            _praxisWebUrl = configuration["PraxisWebUrl"];
            _subscriptionCalculationService = subscriptionCalculationService;
            _praxisClientService = praxisClientService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _securityHelperService = securityHelperService;
            _updateClientSubscriptionInformationService = updateClientSubscriptionInformationService;
        }

        public async Task<bool> InitiateSubscriptionUpdatePaymentProcess(SubscriptionUpdateCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionUpdateService));

            var notificationPayload = new SendPaymnetNotificationPayload
            {
                NotificationSubscriptionId = command.NotificationSubscriptionId,
                ActionName = command.ActionName,
                Context = command.Context,
            };

            try
            {
                var praxisPaymentModuleSeed = PraxisPaymentModuleSeed();
                var previousSubscription = GetPraxisClientSubscription(command.OrganizationId);

                var estimatedNewBill = await _subscriptionUpdateEstimatedBillGenerationService.GenerateSubscriptionUpdateEstimatedBill(
                    command.OrganizationId,
                    null,
                    command.SubscriptionId,
                    PraxisPriceSeed.PraxisPaymentModuleSeedId,
                    command.NumberOfUser - previousSubscription?.NumberOfUser ?? 0,
                    12,
                    0,
                    command.TotalAdditionalStorageInGigaBites - (double)(previousSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0),
                    command.TotalAdditionalTokenInMillion - (double)(previousSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0),
                    command.TotalAdditionalManualTokenInMillion - (double)(previousSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0));

                var paymentModel = GetSubscriptionPaymentModel(estimatedNewBill, praxisPaymentModuleSeed);

                var estimatedTotalBill = GetEstimatedTotalBill(previousSubscription, estimatedNewBill, praxisPaymentModuleSeed);

                var paymentProcessResponse = command.PaymentMode == "OFFLINE" && _securityHelperService.IsAAdmin()
                    ?  new PaymentProcessingResult { PaymentDetailId = Guid.NewGuid().ToString(), RedirectUrl = $"{_praxisWebUrl}/organization/{command.OrganizationId}/billing-details" }
                    : await GetPaymentRedirectionUrl(command, paymentModel);

                if (paymentProcessResponse.StatusCode == 0)
                {

                    await SaveLatestSubscriptionData(
                        command,
                        praxisPaymentModuleSeed,
                        estimatedTotalBill,
                        paymentModel,
                        previousSubscription,
                        paymentProcessResponse.PaymentDetailId);

                    if (command.PaymentMode == "OFFLINE" && _securityHelperService.IsAAdmin())
                    {
                        var updateCommand = new UpdateClientSubscriptionInformationCommand
                        {
                            ClientId = null,
                            OrganizationId = command.OrganizationId,
                            PaymentDetailId = paymentProcessResponse.PaymentDetailId,
                            ActionName = "update-renew-subscription-success",
                            Context = "update-renew-subscription-success",
                            NotificationSubscriptionId = paymentProcessResponse.PaymentDetailId
                        };

                        await _updateClientSubscriptionInformationService.ProcessOfflineUpdateSubscriptionAfterEffects(updateCommand);
                    }

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        Url = paymentProcessResponse.RedirectUrl
                    });

                    await SendPaymnetNotification(notificationPayload, true, denormalizePayload);
                }
                else
                {
                    _logger.LogError("Something went wrong when make payment. Error message: {ErrorMessage} and status code: {StatusCode}.", paymentProcessResponse.ErrorMessage, paymentProcessResponse.StatusCode);

                    await SendPaymnetNotification(notificationPayload, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionUpdateService), ex.Message, ex.StackTrace);

                await SendPaymnetNotification(notificationPayload, false);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionUpdateService));

            return true;
        }

        private PraxisPaymentModuleSeed PraxisPaymentModuleSeed()
        {
            return _repository.GetItem<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
        }

        private SubscriptionPaymentModel GetSubscriptionPaymentModel(
            SubscriptionEstimatedBillResponse estimatedBill,
            PraxisPaymentModuleSeed praxisPaymentModuleSeed)
        {
            var completePackageBill = estimatedBill.PackageCosts.FirstOrDefault(c => c.SubscriptionPackage == "COMPLETE_PACKAGE");

            return new SubscriptionPaymentModel
            {
                Currency = praxisPaymentModuleSeed.DefaultCurrency,
                GrandTotal = estimatedBill.GrandTotal,
                PerUserCost = completePackageBill != null ? completePackageBill.PerUserMonthlyPrice : 0.00,
                AverageCost = completePackageBill != null ? completePackageBill.TotalUserMonthlyPrice : 0.00,
                AdditionalStorageCost = estimatedBill.AdditionalStorageCost,
                AdditionalTokenCost = estimatedBill.AdditionalTokenCost,
                AdditionalManualTokenCost = estimatedBill.AdditionalManualTokenCost,
                TaxDeduction = estimatedBill.TaxAmount,
                DurationOfSubscription = completePackageBill != null ? completePackageBill.DurationOfSubscription : 0,
            };
        }

        private SubscriptionPackage GetCompleteSubcriptionPackageInfo(PraxisPaymentModuleSeed praxisPaymentModuleSeed)
        {
            return praxisPaymentModuleSeed.SubscriptionPackages.FirstOrDefault(x => x.ItemId == PraxisPriceSeed.CompletePackageId);
        }

        private async Task<PaymentProcessingResult> GetPaymentRedirectionUrl(
            SubscriptionUpdateCommand command,
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
            SubscriptionUpdateForClientCommand command,
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
            SubscriptionUpdateCommand command,
            PraxisPaymentModuleSeed praxisPaymentModuleSeed,
            SubscriptionPaymentModel estimatedTotalBill,
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
                    RolesAllowedToRead = previousSubscription.RolesAllowedToRead,
                    IdsAllowedToRead = previousSubscription.IdsAllowedToRead,
                    NumberOfUser = command.NumberOfUser,
                    CreatedUserCount = previousSubscription.CreatedUserCount,
                    DurationOfSubscription = 12,
                    OrganizationType = previousSubscription.OrganizationType,
                    SubscriptionPackage = subscriptionPackageInfo.Title,
                    Location = previousSubscription.Location,
                    PerUserCost = estimatedTotalBill.PerUserCost,
                    AverageCost = estimatedTotalBill.AverageCost,
                    TaxDeduction = estimatedTotalBill.TaxDeduction,
                    GrandTotal = estimatedTotalBill.GrandTotal,
                    PaymentCurrency = estimatedTotalBill.Currency,
                    PaymentHistoryId = paymentHistoryId,
                    OrganizationId = previousSubscription.OrganizationId,
                    OrganizationName = previousSubscription.OrganizationName,
                    OrganizationEmail = previousSubscription.OrganizationEmail,
                    SubscriptionDate = previousSubscription.SubscriptionDate,
                    SubscriptionExpirationDate = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(subsStartDate, previousSubscription.DurationOfSubscription),
                    IsOrgTypeChangeable = true,
                    SubscritionStatus = nameof(PraxisEnums.INITIATED),
                    ModuleList = subscriptionPackageInfo.ModuleList,
                    PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                    PaidAmount = 0,
                    SupportSubscriptionInfo = previousSubscription.SupportSubscriptionInfo,
                    StorageSubscription = PrepareStorageSubscriptionInfo(command, estimatedTotalBill),
                    TokenSubscription = PrepareTokenSubscriptionInfo(command, previousSubscription, estimatedTotalBill),
                    ManualTokenSubscription = PrepareManualTokenSubscriptionInfo(command, previousSubscription, estimatedTotalBill),
                    TotalTokenSubscription = PrepareTotalTokenSubscriptionInfo(command, previousSubscription, estimatedTotalBill),
                    PaymentMode = command.PaymentMode,
                    IsActive = true,
                    NumberOfAuthorizedUsers = (int)command.NumberOfAuthorizedUsers,
                    IsTokenApplied = command.TotalAdditionalTokenInMillion > 0,
                    IsManualTokenApplied = command.TotalAdditionalManualTokenInMillion > 0,
                    SubscriptionInstallments = previousSubscription.SubscriptionInstallments,
                    TotalPerMonthDueCosts = previousSubscription.TotalPerMonthDueCosts,
                    AmountDue = previousSubscription.AmountDue,
                    TaxPercentage = previousSubscription.TaxPercentage,
                    InvoiceMetaData = PrepareInvoiceMetaData(command, previousSubscription, paymentModel),
                };

                await _repository.SaveAsync(clientSubscription);

                _logger.LogInformation("Data has been successfully inserted to {EntityName}.", nameof(PraxisClientSubscription));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception Occured during inserting data to {nameof(PraxisClientSubscription)} entity. " +
                    $"Exception Message: {ex.Message}. " +
                    $"Exception Details: {ex.StackTrace}.");

                return false;
            }
        }

        private async Task<bool> SaveLatestSubscriptionDataForClient(
           SubscriptionUpdateForClientCommand command,
           PraxisPaymentModuleSeed praxisPaymentModuleSeed,
           SubscriptionPaymentModel estimatedTotalBill,
           SubscriptionPaymentModel paymentModel, 
           PraxisClientSubscription previousSubscription,
           string paymentHistoryId)
        {
            try
            {
                var subscriptionPackageInfo = GetCompleteSubcriptionPackageInfo(praxisPaymentModuleSeed);

                DateTime currentTime = DateTime.UtcNow.ToLocalTime();

                var clientSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = currentTime,
                    CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                    LastUpdateDate = currentTime,
                    RolesAllowedToRead = previousSubscription.RolesAllowedToRead,
                    IdsAllowedToRead = previousSubscription.IdsAllowedToRead,
                    NumberOfUser = previousSubscription.NumberOfUser,
                    CreatedUserCount = previousSubscription.CreatedUserCount,
                    DurationOfSubscription = 12,
                    OrganizationType = previousSubscription.OrganizationType,
                    SubscriptionPackage = subscriptionPackageInfo.Title,
                    Location = previousSubscription.Location,
                    TaxDeduction = estimatedTotalBill.TaxDeduction,
                    GrandTotal = estimatedTotalBill.GrandTotal,
                    PaymentCurrency = praxisPaymentModuleSeed.DefaultCurrency,
                    PaymentHistoryId = paymentHistoryId,
                    ClientId = previousSubscription.ClientId,
                    ClientName = previousSubscription.ClientName,
                    ClientEmail = previousSubscription.ClientEmail,
                    SubscriptionDate = previousSubscription.SubscriptionDate,
                    SubscriptionExpirationDate = previousSubscription.SubscriptionExpirationDate,
                    PerUserCost = previousSubscription.PerUserCost,
                    AverageCost = previousSubscription.AverageCost,
                    PaymentMethod = previousSubscription.PaymentMethod,
                    SubscritionStatus = nameof(PraxisEnums.INITIATED),
                    ModuleList = subscriptionPackageInfo.ModuleList,
                    PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                    PaidAmount = 0,
                    SupportSubscriptionInfo = previousSubscription.SupportSubscriptionInfo,
                    StorageSubscription = PrepareStorageSubscriptionInfoForClient(command, previousSubscription, estimatedTotalBill),
                    TokenSubscription = PrepareTokenSubscriptionInfoForClient(command, previousSubscription, estimatedTotalBill),
                    ManualTokenSubscription = PrepareManualTokenSubscriptionInfoForClient(command, previousSubscription, estimatedTotalBill),
                    TotalTokenSubscription = PrepareTotalTokenSubscriptionInfoForClient(command, estimatedTotalBill),
                    PaymentMode = command.PaymentMode,
                    IsActive = true,
                    IsTokenApplied = command.TotalAdditionalTokenInMillion > 0,
                    IsManualTokenApplied = command.TotalAdditionalManualTokenInMillion > 0,
                    AmountDue = previousSubscription.AmountDue,
                    TotalPerMonthDueCosts = previousSubscription.TotalPerMonthDueCosts,
                    TaxPercentage = previousSubscription.TaxPercentage,
                    IsAdditionalAllocation = command.IsAdditionalAllocation,
                    InvoiceMetaData = PrepareInvoiceMetaDataForClient(command, previousSubscription, paymentModel)
                };

                await _repository.SaveAsync(clientSubscription);

                _logger.LogInformation("Data has been successfully inserted to {EntityName}.", nameof(PraxisClientSubscription));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during inserting data to {EntityName} entity. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisClientSubscription), ex.Message, ex.StackTrace);

                return false;
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaData(SubscriptionUpdateCommand command, PraxisClientSubscription previousSubscription, SubscriptionPaymentModel paymentModel)
        {
            var invoiceMetaData = new List<PraxisKeyValue>();

            AddKeyValueIfPositive("NumberOfUser", previousSubscription.NumberOfUser, command?.NumberOfUser ?? 0);
            AddKeyValueIfPositive("AdditionalStorage", previousSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0, command?.TotalAdditionalStorageInGigaBites ?? 0);
            AddKeyValueIfPositive("AdditionalLanguageToken", previousSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0, command?.TotalAdditionalTokenInMillion ?? 0);
            AddKeyValueIfPositive("AdditionalManualToken", previousSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0, command?.TotalAdditionalManualTokenInMillion ?? 0);
            AddStaticKeyValue("DurationOfSubscription", (paymentModel?.DurationOfSubscription ?? 0).ToString("F2"));
            AddStaticKeyValue("TaxDeduction", (paymentModel?.TaxDeduction ?? 0).ToString("F2"));
            AddStaticKeyValue("PerUserCost", (paymentModel?.PerUserCost ?? 0).ToString("F2"));
            AddStaticKeyValue("AverageCost", (paymentModel?.AverageCost ?? 0).ToString("F2"));
            AddStaticKeyValue("InvoiceType", ((int)SubscriptionInvoiceType.Update).ToString());
            AddStaticKeyValue("PaymentMethod", "Annually");
            AddStaticKeyValue("IsOfflineInvoice", command.PaymentMode == "OFFLINE" ? "True" : "False");

            return invoiceMetaData;

            void AddKeyValueIfPositive(string key, double previousValue, double newValue)
            {
                var difference = Math.Abs(newValue - previousValue);
               
                if (difference > 0)
                {
                    invoiceMetaData.Add(new PraxisKeyValue
                    {
                        Key = key,
                        Value = difference.ToString("F2")
                    });
                }
            }

            void AddStaticKeyValue(string key, string value)
            {
                invoiceMetaData.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaDataForClient(SubscriptionUpdateForClientCommand command, PraxisClientSubscription previousSubscription, SubscriptionPaymentModel paymentModel)
        {
            var invoiceMetaData = new List<PraxisKeyValue>();

            AddKeyValueIfPositive("NumberOfUser", 0, 0);
            AddKeyValueIfPositive("AdditionalStorage", previousSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0, command?.TotalAdditionalStorageInGigaBites ?? 0);
            AddKeyValueIfPositive("AdditionalLanguageToken", previousSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0, command?.TotalAdditionalTokenInMillion ?? 0);
            AddKeyValueIfPositive("AdditionalManualToken", previousSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0, command?.TotalAdditionalManualTokenInMillion ?? 0);
            AddStaticKeyValue("DurationOfSubscription", (paymentModel?.DurationOfSubscription ?? 0).ToString("F2"));
            AddStaticKeyValue("TaxDeduction", (paymentModel?.TaxDeduction ?? 0).ToString("F2"));
            AddStaticKeyValue("PerUserCost", (paymentModel?.PerUserCost ?? 0).ToString("F2"));
            AddStaticKeyValue("AverageCost", (paymentModel?.AverageCost ?? 0).ToString("F2"));
            AddStaticKeyValue("InvoiceType", ((int)SubscriptionInvoiceType.Update).ToString());
            AddStaticKeyValue("PaymentMethod", "Annually");

            return invoiceMetaData;

            void AddKeyValueIfPositive(string key, double previousValue, double newValue)
            {
                var difference = Math.Abs(newValue - previousValue);

                if (difference > 0)
                {
                    invoiceMetaData.Add(new PraxisKeyValue
                    {
                        Key = key,
                        Value = difference.ToString("F2")
                    });
                }
            }

            void AddStaticKeyValue(string key, string value)
            {
                invoiceMetaData.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

        private StorageSubscriptionInfo PrepareStorageSubscriptionInfo(SubscriptionUpdateCommand command, SubscriptionPaymentModel paymentModel)
        {
            var storageSubscriptionInfo = new StorageSubscriptionInfo
            {
                IncludedStorageInGigaBites = command.NumberOfUser * 0.5,
                TotalAdditionalStorageInGigaBites = command.TotalAdditionalStorageInGigaBites,
                TotalAdditionalStorageCost = paymentModel.AdditionalStorageCost
            };

            return storageSubscriptionInfo;
        }

        private StorageSubscriptionInfo PrepareStorageSubscriptionInfoForClient(
            SubscriptionUpdateForClientCommand command,
            PraxisClientSubscription previousSubscription,
            SubscriptionPaymentModel paymentModel)
        {
            var storageSubscriptionInfo = new StorageSubscriptionInfo
            {
                IncludedStorageInGigaBites = previousSubscription?.StorageSubscription?.IncludedStorageInGigaBites ?? 0,
                TotalAdditionalStorageInGigaBites = command.TotalAdditionalStorageInGigaBites,
                TotalAdditionalStorageCost = paymentModel.AdditionalStorageCost,
                IsAllocatedFromOrganization = command.IsAdditionalAllocation
            };

            return storageSubscriptionInfo;
        }

        private TokenSubscriptionInfo PrepareTokenSubscriptionInfo(
            SubscriptionUpdateCommand command, 
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

        private ManualTokenSubscriptionInfo PrepareManualTokenSubscriptionInfo(
           SubscriptionUpdateCommand command,
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

        private TotalTokenSubscriptionInfo PrepareTotalTokenSubscriptionInfo(
          SubscriptionUpdateCommand command,
          PraxisClientSubscription previousSubscription,
          SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new TotalTokenSubscriptionInfo
            {
                TotalTokenInMillion = command.TotalAdditionalTokenInMillion + command.TotalAdditionalManualTokenInMillion,
                TotalTokenCost = paymentModel.AdditionalTokenCost + paymentModel.AdditionalManualTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private TokenSubscriptionInfo PrepareTokenSubscriptionInfoForClient(
            SubscriptionUpdateForClientCommand command, 
            PraxisClientSubscription previousSubscription, 
            SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new TokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.TokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalTokenInMillion,
                TotalAdditionalTokenCost = paymentModel.AdditionalTokenCost,
                IsAllocatedFromOrganization = command.IsAdditionalAllocation
            };

            return tokenSubscriptionInfo;
        }

        private ManualTokenSubscriptionInfo PrepareManualTokenSubscriptionInfoForClient(
            SubscriptionUpdateForClientCommand command,
            PraxisClientSubscription previousSubscription,
            SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new ManualTokenSubscriptionInfo
            {
                IncludedTokenInMillion = previousSubscription?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0,
                TotalAdditionalTokenInMillion = command.TotalAdditionalManualTokenInMillion,
                TotalAdditionalTokenCost = paymentModel.AdditionalManualTokenCost,
                IsAllocatedFromOrganization = command.IsAdditionalAllocation 
            };

            return tokenSubscriptionInfo;
        }

        private TotalTokenSubscriptionInfo PrepareTotalTokenSubscriptionInfoForClient(
           SubscriptionUpdateForClientCommand command,
           SubscriptionPaymentModel paymentModel)
        {
            var tokenSubscriptionInfo = new TotalTokenSubscriptionInfo
            {
                TotalTokenInMillion = command.TotalAdditionalTokenInMillion + command.TotalAdditionalManualTokenInMillion,
                TotalTokenCost = paymentModel.AdditionalTokenCost + paymentModel.AdditionalManualTokenCost
            };

            return tokenSubscriptionInfo;
        }

        private async Task SendPaymnetNotification(SendPaymnetNotificationPayload command, bool response, string denormalizePyload = null)
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

        private SubscriptionPaymentModel GetEstimatedTotalBill(
            PraxisClientSubscription previousSubscription,
            SubscriptionEstimatedBillResponse estimatedBill,
            PraxisPaymentModuleSeed praxisPaymentModuleSeed)
        {
            var newEstimatedBill = estimatedBill.PackageCosts.FirstOrDefault(c => c.SubscriptionPackage == "COMPLETE_PACKAGE");

            var totalPackagePrice = previousSubscription.AverageCost * previousSubscription.DurationOfSubscription + (newEstimatedBill != null ? newEstimatedBill.CalculatedPrice : 0);
            var totalNumberOfUser = previousSubscription.NumberOfUser + (newEstimatedBill != null ? newEstimatedBill.TotalUserNumber : 0);
            var durationOfSubscription = previousSubscription.DurationOfSubscription;

            return new SubscriptionPaymentModel
            {
                Currency = praxisPaymentModuleSeed.DefaultCurrency,

                AdditionalStorageCost =
                (double)
                (previousSubscription?.StorageSubscription?.TotalAdditionalStorageCost != null ? previousSubscription.StorageSubscription.TotalAdditionalStorageCost : 0)
                +
                estimatedBill.AdditionalStorageCost,

                AdditionalTokenCost =
                (double)
                (previousSubscription?.TokenSubscription?.TotalAdditionalTokenCost ?? 0)
                +
                estimatedBill.AdditionalTokenCost,

                SupportSubscriptionCost =
                (double)
                (previousSubscription?.SupportSubscriptionInfo?.TotalSupportCost ?? 0)
                +
                estimatedBill.SupportSubscriptionCost,

                AverageCost = totalPackagePrice / durationOfSubscription,
                PerUserCost = totalNumberOfUser > 0 ? totalPackagePrice / (durationOfSubscription * totalNumberOfUser) : 0,

                GrandTotal = previousSubscription.GrandTotal + estimatedBill.GrandTotal,
                TaxDeduction = previousSubscription.TaxDeduction + estimatedBill.TaxAmount
            };
        }

        public async Task<bool> InitiateSubscriptionUpdatePaymentProcessForClient(SubscriptionUpdateForClientCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionUpdateService));

            var notificationPayload = new SendPaymnetNotificationPayload
            {
                NotificationSubscriptionId = command.NotificationSubscriptionId,
                ActionName = command.ActionName,
                Context = command.Context,
            };

            try
            {
                var praxisPaymentModuleSeed = PraxisPaymentModuleSeed();
                var previousSubscription = GetPraxisClientSubscriptionForClient(command.ClientId);

                var estimatedNewBill = await _subscriptionUpdateEstimatedBillGenerationService.GenerateSubscriptionUpdateEstimatedBill(
                    null,
                    command.ClientId,
                    command.SubscriptionId,
                    PraxisPriceSeed.PraxisPaymentModuleSeedId,
                    0,
                    12,
                    0,
                    command.TotalAdditionalStorageInGigaBites - (double)(previousSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0),
                    command.TotalAdditionalTokenInMillion - (double)(previousSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0), command.TotalAdditionalManualTokenInMillion - (double)(previousSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0));

                var paymentModel = GetSubscriptionPaymentModel(estimatedNewBill, praxisPaymentModuleSeed);

                var estimatedTotalBill = GetEstimatedTotalBill(previousSubscription, estimatedNewBill, praxisPaymentModuleSeed);

                var paymentProcessResponse = await GetPaymentRedirectionUrlForClient(command, paymentModel);

                if (paymentProcessResponse?.StatusCode == 0)
                {
                    await SaveLatestSubscriptionDataForClient(
                        command,
                        praxisPaymentModuleSeed,
                        estimatedTotalBill,
                        paymentModel,
                        previousSubscription,
                        paymentProcessResponse?.PaymentDetailId);

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        Url = paymentProcessResponse?.RedirectUrl
                    });

                    await SendPaymnetNotification(notificationPayload, true, denormalizePayload);
                }
                else
                {
                    _logger.LogError("Something went wrong when make payment. Error message: {ErrorMessage} and status code: {StatusCode}.", paymentProcessResponse?.ErrorMessage, paymentProcessResponse?.StatusCode);

                    await SendPaymnetNotification(notificationPayload, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionUpdateService), ex.Message, ex.StackTrace);

                await SendPaymnetNotification(notificationPayload, false);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionUpdateService));

            return true;
        }

        public async Task<bool> InitiateSubscriptionUpdateForAllocation(SubscriptionUpdateForClientCommand command)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(SubscriptionUpdateService));

            try
            {
                if (command.IsAdditionalAllocation)
                {
                    var clientSubscription = GetPraxisClientSubscriptionForClient(command.ClientId);
                    if (clientSubscription != null)
                    {
                        var praxisPaymentModuleSeed = await _subscriptionCalculationService.GetSubscriptionSeedData(PraxisPriceSeed.PraxisPaymentModuleSeedId);
                        var totalAdditionalStorage = command?.TotalAdditionalStorageInGigaBites ?? clientSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0;
                        var totalAdditionalStorageCost = (double)((praxisPaymentModuleSeed?.StorageSubscriptionSeed?.PricePerGigaBiteStorage ?? 0) * totalAdditionalStorage);
                        var totalAdditionalLanguagesToken = command?.TotalAdditionalTokenInMillion ?? clientSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0;
                        var totalAdditionalLanguagesTokenCost = (double)((praxisPaymentModuleSeed?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0) * totalAdditionalLanguagesToken);
                        var totalAdditionalManualToken = command?.TotalAdditionalManualTokenInMillion ?? clientSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0;
                        var totalAdditionalManualTokenCost = (double)((praxisPaymentModuleSeed?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0) * totalAdditionalManualToken);

                        UpdateClientSubscriptionDetails(clientSubscription, totalAdditionalStorage, totalAdditionalStorageCost,
                                                         totalAdditionalLanguagesToken, totalAdditionalLanguagesTokenCost,
                                                         totalAdditionalManualToken, totalAdditionalManualTokenCost);

                        await _repository.UpdateAsync(s => s.ItemId == clientSubscription.ItemId, clientSubscription);

                        await _praxisClientService.UpdateClientSubscriptionRelatedData(
                            command.ClientId,
                            command.TotalAdditionalStorageInGigaBites,
                            command.TotalAdditionalTokenInMillion,
                            command.TotalAdditionalManualTokenInMillion);

                        await _departmentSubscriptionService.SaveDepartmentSubscription(command.ClientId, clientSubscription);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(SubscriptionUpdateService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(SubscriptionUpdateService));

            return true;
        }

        private static void UpdateClientSubscriptionDetails(
            PraxisClientSubscription clientSubscription,
            double totalAdditionalStorage, double totalAdditionalStorageCost,
            double totalAdditionalLanguagesToken, double totalAdditionalLanguagesTokenCost,
            double totalAdditionalManualToken, double totalAdditionalManualTokenCost)
        {
            clientSubscription.StorageSubscription ??= new StorageSubscriptionInfo();
            clientSubscription.TokenSubscription ??= new TokenSubscriptionInfo();
            clientSubscription.ManualTokenSubscription ??= new ManualTokenSubscriptionInfo();
            clientSubscription.TotalTokenSubscription ??= new TotalTokenSubscriptionInfo();

            clientSubscription.StorageSubscription.TotalAdditionalStorageInGigaBites = totalAdditionalStorage;
            clientSubscription.StorageSubscription.TotalAdditionalStorageCost = totalAdditionalStorageCost;

            clientSubscription.TokenSubscription.TotalAdditionalTokenInMillion = totalAdditionalLanguagesToken;
            clientSubscription.TokenSubscription.TotalAdditionalTokenCost = totalAdditionalLanguagesTokenCost;

            clientSubscription.ManualTokenSubscription.TotalAdditionalTokenInMillion = totalAdditionalManualToken;
            clientSubscription.ManualTokenSubscription.TotalAdditionalTokenCost = totalAdditionalManualTokenCost;

            clientSubscription.TotalTokenSubscription.TotalTokenInMillion = totalAdditionalLanguagesToken + totalAdditionalManualToken;
            clientSubscription.TotalTokenSubscription.TotalTokenCost = totalAdditionalLanguagesTokenCost + totalAdditionalManualTokenCost;

            clientSubscription.TokenSubscription.IsAllocatedFromOrganization = totalAdditionalLanguagesToken > 0;
            clientSubscription.ManualTokenSubscription.IsAllocatedFromOrganization = totalAdditionalManualToken > 0;
            clientSubscription.StorageSubscription.IsAllocatedFromOrganization = totalAdditionalStorage > 0;

            clientSubscription.IsTokenApplied = totalAdditionalLanguagesToken > 0;
            clientSubscription.IsManualTokenApplied = totalAdditionalManualToken > 0;
            clientSubscription.IsAdditionalAllocation = true;
        }

    }
}