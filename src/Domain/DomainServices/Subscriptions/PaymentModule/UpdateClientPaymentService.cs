using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class UpdateClientPaymentService : IUpdateClientPaymentService
    {
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly ILogger<UpdateClientPaymentService> _logger;
        private readonly INotificationService _notificationProviderService;
        private readonly string _paymentFailUrl;

        private readonly string _paymentServiceBaseUrl;
        private readonly string _paymentServiceInitializeUrl;
        private readonly string _paymentServiceVersion;
        private readonly string _praxisWebUrl;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly ISubscriptionCalculationService _subscriptionCalculationService;

        public UpdateClientPaymentService(
            ILogger<UpdateClientPaymentService> logger,
            IRepository repository,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider,
            IServiceClient serviceClient,
            INotificationService notificationProviderService,
            ICommonUtilService commonUtilService,
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
            _paymentServiceBaseUrl = configuration["PaymentServiceBaseUrl"];
            _paymentServiceVersion = configuration["PaymentServiceVersion"];
            _paymentServiceInitializeUrl = configuration["PaymentServiceInitializeUrl"];
            _paymentFailUrl = configuration["PaymentFailUrl"];
            _praxisWebUrl = configuration["PraxisWebUrl"];
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _subscriptionCalculationService = subscriptionCalculationService;
        }

        public async Task UpdatePaymentInformation(UpdateClientPaymentCommand command)
        {
            _logger.LogInformation("Going to update client payment information with command: {Command}", JsonConvert.SerializeObject(command));

            try
            {
                var paymentProcessResponse = await GetPaymentRedirectionUrl(command);
                if (paymentProcessResponse.StatusCode == 0)
                {
                    var response = SaveSubscriptionData(command, paymentProcessResponse.PaymentDetailId);
                    var result = new
                    {
                        NotifiySubscriptionId = command.NotificationSubscriptionId,
                        Success = response,
                        command.NotificationSubscriptionId
                    };

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        Url = paymentProcessResponse.RedirectUrl,
                    });

                    await _notificationProviderService.PaymentNotification(response, command.NotificationSubscriptionId,
                        result, command.Context, command.ActionName, denormalizePayload);
                }
                else
                {
                    var result = new
                    {
                        NotifiySubscriptionId = command.NotificationSubscriptionId,
                        Success = false,
                        command.NotificationSubscriptionId
                    };
                    await _notificationProviderService.PaymentNotification(false, command.NotificationSubscriptionId,
                        result, command.Context, command.ActionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during processing payment data with command: {Command}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    JsonConvert.SerializeObject(command), ex.Message, ex.StackTrace);
                var result = new
                {
                    NotifiySubscriptionId = command.NotificationSubscriptionId,
                    Success = false,
                    command.NotificationSubscriptionId
                };
                await _notificationProviderService.PaymentNotification(false, command.NotificationSubscriptionId,
                    result, command.Context, command.ActionName);
            }
        }

        private async Task<PaymentProcessingResult> GetPaymentRedirectionUrl(UpdateClientPaymentCommand command)
        {
            var paymentMicroserviceModel = new PaymentMicroserviceModel
            {
                ProviderName = "SIX",
                Amount = command.AmountToPay,
                CurrencyCode = command.PaymentCurrency,
                OrderId = Guid.NewGuid().ToString(),
                Description = "Order Description",
                NotificationUrl = _praxisWebUrl + "/api/business-praxismonitor/PraxisMonitorWebService/PraxisMonitorQuery/ValidateUpdatePayment",
                SuccessUrl = $"{_praxisWebUrl}/organization/{command.OrganizationId}/billing-details",
                FailUrl = $"{_praxisWebUrl}/{_paymentFailUrl}"
            };
            var token = await GetAdminToken();

            var response = await _serviceClient.SendToHttpAsync<PaymentProcessingResult>(
                HttpMethod.Post,
                _paymentServiceBaseUrl,
                _paymentServiceVersion,
                _paymentServiceInitializeUrl,
                paymentMicroserviceModel,
                token);

            if (response.StatusCode != 0)
            {
                _logger.LogError("Error occurred during initiate payment by payment service. Error: {Response} and exception -> {ErrorMessage}",
                    JsonConvert.SerializeObject(response), response.ErrorMessage);
            }
            return response;
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
                Roles = new List<string> {RoleNames.Admin, RoleNames.SystemAdmin}
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        private bool SaveSubscriptionData(UpdateClientPaymentCommand command, string paymentHistoryId)
        {
            try
            {
                var existingClientSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.ItemId == command.SubscriptionId && s.OrganizationId == command.OrganizationId && s.IsLatest);

                if (existingClientSubscription != null)
                {
                    var clientSubscription = new PraxisClientSubscription
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        RolesAllowedToRead = existingClientSubscription.RolesAllowedToRead,
                        IdsAllowedToRead = existingClientSubscription.IdsAllowedToRead,
                        NumberOfUser = existingClientSubscription.NumberOfUser,
                        CreatedUserCount = existingClientSubscription.CreatedUserCount,
                        DurationOfSubscription = existingClientSubscription.DurationOfSubscription,
                        OrganizationType = existingClientSubscription.OrganizationType,
                        SubscriptionPackage = existingClientSubscription.SubscriptionPackage,
                        Location = existingClientSubscription.Location,
                        PerUserCost = existingClientSubscription.PerUserCost,
                        AverageCost = existingClientSubscription.AverageCost,
                        TaxDeduction = existingClientSubscription.TaxDeduction,
                        GrandTotal = existingClientSubscription.GrandTotal + command.AmountToPay,
                        PaymentCurrency = command.PaymentCurrency,
                        PaymentHistoryId = paymentHistoryId,
                        OrganizationId = existingClientSubscription.OrganizationId,
                        OrganizationName = existingClientSubscription.OrganizationName,
                        OrganizationEmail = existingClientSubscription.OrganizationEmail,
                        SubscriptionDate = existingClientSubscription.SubscriptionDate,
                        SubscriptionExpirationDate = existingClientSubscription.SubscriptionExpirationDate,
                        IsOrgTypeChangeable = true,
                        SubscritionStatus = nameof(PraxisEnums.INITIATED),
                        ModuleList = existingClientSubscription.ModuleList,
                        PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                        PaidAmount = 0,
                        SupportSubscriptionInfo = existingClientSubscription.SupportSubscriptionInfo,
                        StorageSubscription = existingClientSubscription.StorageSubscription,
                        TokenSubscription = existingClientSubscription.TokenSubscription,
                        ManualTokenSubscription = existingClientSubscription.ManualTokenSubscription,
                        TotalTokenSubscription = existingClientSubscription.TotalTokenSubscription,
                        PaymentMode = existingClientSubscription.PaymentMode,
                        IsActive = true,
                        NumberOfAuthorizedUsers = existingClientSubscription.NumberOfAuthorizedUsers,
                        IsTokenApplied = existingClientSubscription.IsTokenApplied,
                        SubscriptionInstallments = GetSubscriptionInstallments(command, existingClientSubscription),
                        TotalPerMonthDueCosts = existingClientSubscription.TotalPerMonthDueCosts,
                        AmountDue = command.AmountDue,
                        InvoiceMetaData = PrepareInvoiceMetaData(command)
                    };

                    _repository.Save(clientSubscription);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName}.", nameof(PraxisClientSubscription));
                }
                else
                {
                    _logger.LogInformation("No existing project client subscription found");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during inserting data to {EntityName} entity. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                nameof(PraxisClientSubscription), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaData(UpdateClientPaymentCommand command)
        {
            string paymentMethod = _subscriptionCalculationService.GetSubscriptionPaymentMethod(command.PaidDuration);
            var invoiceMetaData = new List<PraxisKeyValue>
            {
                new PraxisKeyValue
                {
                    Key = "InvoiceType",
                    Value = ((int)SubscriptionInvoiceType.DuePayment).ToString()
                },
                new PraxisKeyValue
                {
                    Key = "PaymentMethod",
                    Value = paymentMethod
                }
            };

            return invoiceMetaData;
        }

        private List<SubscriptionInstallment> GetSubscriptionInstallments(UpdateClientPaymentCommand command, PraxisClientSubscription existingClientSubscription)
        {
            var installments = existingClientSubscription?.SubscriptionInstallments ?? new List<SubscriptionInstallment>();
            var paidDurationSum = installments.Select(s => s.PaidDuration).Sum() + command.PaidDuration;

            var installment = new SubscriptionInstallment()
            {
                PaidDuration = command.PaidDuration,
                EndOfActivePeriod = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(existingClientSubscription.SubscriptionDate, paidDurationSum),
                PaidAmount = command.AmountToPay
            };

            installments.Add(installment);

            return installments;
        }

        private SubscriptionPackage GetSubscriptionPackageInfo(string subscriptionPackageId)
        {
            var subcriptionPackageInfo = _repository.GetItem<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
            if (subcriptionPackageInfo == null)
            {
                return null;
            }
            return subcriptionPackageInfo.SubscriptionPackages.FirstOrDefault(x => x.ItemId == subscriptionPackageId);
        }
    }
}