using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class UpdateClientSubscriptionInformationService : IUpdateClientSubscriptionInformation
    {
        private readonly ILogger<UpdateClientSubscriptionInformationService> _logger;
        private readonly IRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly IActivateUserAccount _activateUserAccountService;
        private readonly IProcessPaymentInvoiceService _processPaymentInvoiceService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IEmailDataBuilder _emailDataBuilder;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly INavigationPreparationTypeStrategy _navigationPreparationTypeStrategy;
        private readonly IProcessClientData _processClientDataService;
        private readonly IPraxisClientCustomSubscriptionService _praxisClientCustomSubscriptionService;
        private readonly ILincensingService _lincensingService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IPraxisRenewSubscriptionService _praxisRenewSubscriptionService;
        private readonly ISubscriptionRenewalService _subscriptionRenewalService;
        private readonly ISecurityHelperService _securityHelperService;

        public ILincensingService LincensingService { get; }

        public UpdateClientSubscriptionInformationService(
            ILogger<UpdateClientSubscriptionInformationService> logger,
            IRepository repository,
            INotificationService notificationService,
            IActivateUserAccount activateUserAccountService,
            IProcessPaymentInvoiceService processPaymentInvoiceService,
            ISecurityContextProvider securityContextProvider,
            IEmailDataBuilder emailDataBuilder,
            IEmailNotifierService emailNotifierService,
            IUilmResourceKeyService uilmResourceKeyService,
            INavigationPreparationTypeStrategy navigationPreparationTypeStrategy,
            IProcessClientData processClientDataService,
            IPraxisClientCustomSubscriptionService praxisClientCustomSubscriptionService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IOrganizationSubscriptionService organizationSubscriptionService,
            ILincensingService lincensingService,
            IPraxisRenewSubscriptionService praxisRenewSubscriptionService,
            ISubscriptionRenewalService subscriptionRenewalService,
            ISecurityHelperService securityHelperService
        )
        {
            _logger = logger;
            _repository = repository;
            _notificationService = notificationService;
            _activateUserAccountService = activateUserAccountService;
            _processPaymentInvoiceService = processPaymentInvoiceService;
            _securityContextProvider = securityContextProvider;
            _emailDataBuilder = emailDataBuilder;
            _emailNotifierService = emailNotifierService;
            _uilmResourceKeyService = uilmResourceKeyService;
            _navigationPreparationTypeStrategy = navigationPreparationTypeStrategy;
            _processClientDataService = processClientDataService;
            _praxisClientCustomSubscriptionService = praxisClientCustomSubscriptionService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _organizationSubscriptionService = organizationSubscriptionService;
            _lincensingService = lincensingService;
            _praxisRenewSubscriptionService = praxisRenewSubscriptionService;
            _subscriptionRenewalService = subscriptionRenewalService;
            _securityHelperService = securityHelperService;
        }

        public async Task ProcessUpdateRenewSubscriptionAfterEffectsForOrg(UpdateClientSubscriptionInformationCommand command)
        {
            _logger.LogInformation("Going to update subscription data for organizationId -> {OrganizationId}", command.OrganizationId);
            try
            {
                var currentSubscriptionData = await _praxisClientSubscriptionService.GetSubscriptionDataByPaymentDetailId(command.PaymentDetailId);

                if (currentSubscriptionData != null)
                {
                    if (currentSubscriptionData.IsLatest)
                    {
                        _logger.LogInformation("Going to update subscription data for organizationId -> {OrganizationId}", command.OrganizationId);
                        return;
                    }

                    var previousSubscription = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(currentSubscriptionData.OrganizationId);

                    if (currentSubscriptionData.IsActive)
                    {
                        var isSubscriptionDataUpdateSucccess = await UpdatePreviousSubscription(previousSubscription);
                        isSubscriptionDataUpdateSucccess &= await UpdateCurrentSubscription(currentSubscriptionData, previousSubscription);
                        if (isSubscriptionDataUpdateSucccess)
                        {
                            await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                        }

                        await ProcessSubscriptionInfoAsync(currentSubscriptionData, currentSubscriptionData.OrganizationId);
                        await ProcessClientRenewSubscriptionAfterEffectsAsync(currentSubscriptionData);
                    }
                    else
                    {
                        await UpdateSubscriptionRenewalData(currentSubscriptionData);
                        await RenewSubscriptionIfAlreadyExpired(command, currentSubscriptionData);
                        await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                    }

                    await SendInvoice(currentSubscriptionData, command.PaymentDetailId);
                    await SendEmailToSubscriptionUpdateForOrg(command.OrganizationId, currentSubscriptionData, previousSubscription);
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with PaymentHistoryId: {PaymentDetailId}.", nameof(PraxisClientSubscription), command.PaymentDetailId);
                    await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update client subscription information. Exception message: {ErrorMessage}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
                await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
            }
        }

        public async Task ProcessClientRenewSubscriptionAfterEffectsAsync(PraxisClientSubscription currentSubscriptionData)
        {
            var praxisClients = _repository
                .GetItems<PraxisClient>(pc => pc.ParentOrganizationId == currentSubscriptionData.OrganizationId && !pc.IsMarkedToDelete)
                .ToList();

            var eligibleClients = praxisClients
                .Where(client =>
                    (client.AdditionalLanguagesToken ?? 0) > 0 ||
                    (client.AdditionalManualToken ?? 0) > 0 ||
                    (client.AdditionalStorage ?? 0) > 0
                )
                .ToList();

            var renewalTasks = eligibleClients
                .Select(client => InitiateClientRenewSubscriptionAfterEffectsAsync(client.ItemId, currentSubscriptionData));

            await Task.WhenAll(renewalTasks);
        }

        private async Task InitiateClientRenewSubscriptionAfterEffectsAsync(string clientId, PraxisClientSubscription currentOrgSubscriptionData)
        {
            string paymentDetailId = Guid.NewGuid().ToString();

            var command = new SubscriptionRenewalForClientCommand
            {
                ClientId = clientId,
                TotalAdditionalTokenInMillion = currentOrgSubscriptionData?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0,
                TotalAdditionalManualTokenInMillion = currentOrgSubscriptionData?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0,
                TotalAdditionalStorageInGigaBites = currentOrgSubscriptionData?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0,
                DurationOfSubscription = 12,
                PaidDuration = 12,
                PaymentMode = currentOrgSubscriptionData.PaymentMode,
                IsTokenApplied = currentOrgSubscriptionData?.IsTokenApplied ?? false,
                IsManualTokenApplied = currentOrgSubscriptionData?.IsAdditionalAllocation ?? false
            };
            await _subscriptionRenewalService.InitiateSubscriptionRenewalPaymentProcessAsync(paymentDetailId, command);

            var updateCommand = new UpdateClientSubscriptionInformationCommand
            {
                ClientId = clientId,
                OrganizationId = null,
                PaymentDetailId = paymentDetailId,
                ActionName = "update-renew-subscription-success",
                Context = "update-renew-subscription-success",
                NotificationSubscriptionId = paymentDetailId
            };
            await ProcessUpdateRenewSubscriptionAfterEffectsForClient(updateCommand);
        }

        public async Task ProcessUpdateRenewSubscriptionAfterEffectsForClient(UpdateClientSubscriptionInformationCommand command)
        {
            _logger.LogInformation("Going to update subscription data for clientId -> {ClientId}", command.ClientId);
            try
            {
                var currentSubscriptionData = await _praxisClientSubscriptionService.GetSubscriptionDataByPaymentDetailId(command.PaymentDetailId);

                if (currentSubscriptionData != null)
                {
                    if (currentSubscriptionData.IsLatest)
                    {
                        _logger.LogInformation("Subscription info already updated with clientId -> {ClientId}", currentSubscriptionData.ClientId);
                        return;
                    }

                    var previousSubscription = await _praxisClientSubscriptionService.GetClientLatestSubscriptionData(currentSubscriptionData.ClientId);

                    if (currentSubscriptionData.IsActive)
                    {
                        var isSubscriptionDataUpdateSucccess = await UpdatePreviousSubscription(previousSubscription);
                        isSubscriptionDataUpdateSucccess &= await UpdateCurrentSubscription(currentSubscriptionData, previousSubscription);
                        if (isSubscriptionDataUpdateSucccess)
                        {
                            await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                        }

                        var praxisClient = await _repository.GetItemAsync<PraxisClient>(pc => pc.ItemId == currentSubscriptionData.ClientId);
                        await ProcessSubscriptionInfoAsync(currentSubscriptionData, praxisClient?.ParentOrganizationId);
                    }
                    else
                    {
                        await UpdateSubscriptionRenewalData(currentSubscriptionData);
                        await RenewSubscriptionIfAlreadyExpired(command, currentSubscriptionData);
                        await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                    }

                    await SendInvoice(currentSubscriptionData, command.PaymentDetailId);
                    await SendEmailToSubscriptionUpdateForClient(command.ClientId, currentSubscriptionData, previousSubscription);
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with PaymentHistoryId: {PaymentHistoryId}.", nameof(PraxisClientSubscription), command.PaymentDetailId);
                    await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update client subscription information. Exception message: {ErrorMessage}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
                await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
            }
        }

        public async Task RenewSubscriptionIfAlreadyExpired(UpdateClientSubscriptionInformationCommand command, PraxisClientSubscription currentSubscription, string notificationId = null)
        {
            PraxisClientSubscriptionNotification activeSubsNotification = null;
            var orgId = command.OrganizationId;
            if (!string.IsNullOrEmpty(orgId))
            {
                activeSubsNotification = await _praxisRenewSubscriptionService.GetOrganizationCurrentSubscriptionNotificationData(command.OrganizationId);
            }
            else if (!string.IsNullOrEmpty(command.ClientId))
            {
                activeSubsNotification = await _praxisRenewSubscriptionService.GetClientCurrentSubscriptionNotificationData(command.ClientId);
                var praxisClient = await _repository.GetItemAsync<PraxisClient>(pc => pc.ItemId == command.ClientId);
                orgId = praxisClient?.ParentOrganizationId;
            }

            if (string.IsNullOrEmpty(orgId))
            {
                _logger.LogError("No Org Id found");
                return;
            }

            if (activeSubsNotification == null)
            {
                await _praxisClientSubscriptionService.UpdateExpiredSubscriptionNotificationData(command.OrganizationId, command.ClientId);
                await _praxisClientSubscriptionService.UpdateExpiredSubscriptionData(currentSubscription.ItemId, command.OrganizationId, command.ClientId);
                await _praxisClientSubscriptionService.UpdateSubscriptionRenewalData(currentSubscription);

                if (!string.IsNullOrEmpty(notificationId))
                {
                    await _praxisClientSubscriptionService.UpdateSubscriptionRenewalNotificationData(notificationId);
                }
                else
                {
                    await _praxisClientSubscriptionService.SaveSubscriptionNotification(command.OrganizationId, currentSubscription, activeSubsNotification == null);
                }

                await ProcessSubscriptionInfoAsync(currentSubscription, orgId);
            }
            else
            {
                await _praxisClientSubscriptionService.SaveSubscriptionNotification(command.OrganizationId, currentSubscription, activeSubsNotification == null);
            }
            
        }

        public async Task ProcessSubscriptionInfoAsync(PraxisClientSubscription currentSubscriptionData, string organizationId)
        {
            if (!string.IsNullOrEmpty(organizationId))
            {
                var praxisClients = _repository
                    .GetItems<PraxisClient>(pc => pc.ParentOrganizationId == organizationId && !pc.IsMarkedToDelete)
                    .ToList();

                var praxisClientIds = praxisClients.Select(pc => pc.ItemId).ToList();

                var clientSubscriptions = _repository
                    .GetItems<PraxisClientSubscription>(pcs => praxisClientIds.Contains(pcs.ClientId) && pcs.IsActive && pcs.IsLatest && pcs.SubscriptionExpirationDate > DateTime.UtcNow)
                    .ToList();

                var clientStorageSubscriptionsSum = clientSubscriptions
                    .Sum(cs => cs.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0);

                var clientTokenSubscriptionsSum = clientSubscriptions
                    .Sum(cs => cs.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

                var clientManualTokenSubscriptionsSum = clientSubscriptions
                    .Sum(cs => cs.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

                var currOrgSubscription = _repository
                    .GetItem<PraxisClientSubscription>(pcs => pcs.OrganizationId == organizationId && pcs.IsActive && pcs.IsLatest);

                var includedTokenInMillion = currOrgSubscription.TokenSubscription?.IncludedTokenInMillion ?? 0;
                var totalAdditionalTokenInMillion = currOrgSubscription.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0;
                var includedManualTokenInMillion = currOrgSubscription.ManualTokenSubscription?.IncludedTokenInMillion ?? 0;
                var totalAdditionalManualTokenInMillion = currOrgSubscription.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0;

                var totalToken = includedTokenInMillion + totalAdditionalTokenInMillion + clientTokenSubscriptionsSum;
                var totalManualToken = includedManualTokenInMillion + totalAdditionalManualTokenInMillion + clientManualTokenSubscriptionsSum;

                var includedStorageInGigaBites = currOrgSubscription.StorageSubscription?.IncludedStorageInGigaBites ?? 0;
                var totalAdditionalStorageInGigaBites = currOrgSubscription.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0;
                var totalStorage = includedStorageInGigaBites + totalAdditionalStorageInGigaBites + clientStorageSubscriptionsSum;

                await _lincensingService.ProcessStorageLicensing(organizationId, (double)totalStorage);

                await UpdateDepartmentSubscription(currentSubscriptionData);

                var organizationSubsPayload = new OrganizationSubscription
                {
                    OrganizationId = organizationId,
                    TotalTokenSize = totalToken,
                    TotalStorageSize = (double)totalStorage,
                    TokenOfOrganization = includedTokenInMillion + totalAdditionalTokenInMillion,
                    StorageOfOrganization = includedStorageInGigaBites + totalAdditionalStorageInGigaBites,
                    TokenOfUnits = clientTokenSubscriptionsSum,
                    StorageOfUnits = clientStorageSubscriptionsSum,
                    SubscriptionDate = currentSubscriptionData.SubscriptionDate,
                    SubscriptionExpirationDate = currentSubscriptionData.SubscriptionExpirationDate,
                    IsTokenApplied = currentSubscriptionData.IsTokenApplied,
                    TotalManualTokenSize = totalManualToken,
                    ManualTokenOfOrganization = includedManualTokenInMillion + totalAdditionalManualTokenInMillion,
                    ManualTokenOfUnits = clientManualTokenSubscriptionsSum,
                    IsManualTokenApplied = currentSubscriptionData.IsManualTokenApplied
                };
                await UpdateOrganizationUserLimit(currentSubscriptionData);
                await UpdateOrganizationSubscription(organizationSubsPayload);
            }
        }

        private async Task<bool> UpdatePreviousSubscription(PraxisClientSubscription previousSubscription)
        {
            var identifier = !string.IsNullOrEmpty(previousSubscription?.OrganizationId)
                     ? $"organizationId: {previousSubscription?.OrganizationId}"
                     : $"clientId: {previousSubscription?.ClientId}";
            try
            {
                if (previousSubscription != null)
                {
                    var updates = new Dictionary<string, object>
                    {
                        {"IsLatest", false},
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()}
                    };

                    await _repository.UpdateAsync<PraxisClientSubscription>(pcs => pcs.ItemId == previousSubscription.ItemId, updates);

                    _logger.LogInformation(
                        $"Removed previous subscription from latest for {identifier}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update previous subscription data for {Identifier}. Exception Message: {Message}. Exception Details: {StackTrace}.", identifier, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdateCurrentSubscription(PraxisClientSubscription subscriptionData, PraxisClientSubscription previousSubscription)
        {
            var identifier = !string.IsNullOrEmpty(previousSubscription?.OrganizationId)
                     ? $"organizationId: {previousSubscription?.OrganizationId}"
                     : $"clientId: {previousSubscription?.ClientId}";
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"SubscritionStatus", nameof(PraxisEnums.ONGOING) },
                        {"PaidAmount", subscriptionData.GrandTotal - previousSubscription.GrandTotal},
                        {"IsLatest", true},
                    };

                await _repository.UpdateAsync<PraxisClientSubscription>(pcs => pcs.ItemId == subscriptionData.ItemId, updates);

                _logger.LogInformation(
                    $"Made current subscription latest for {identifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update current subscription data for {Identifier}. Exception Message: {Message}. Exception Details: {StackTrace}.", identifier, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdateSubscriptionRenewalData(PraxisClientSubscription subscriptionData)
        {
            var identifier = !string.IsNullOrEmpty(subscriptionData?.OrganizationId)
                     ? $"organizationId: {subscriptionData?.OrganizationId}"
                     : $"clientId: {subscriptionData?.ClientId}";
            try
            {
                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"PaidAmount", subscriptionData.GrandTotal},
                    };

                await _repository.UpdateAsync<PraxisClientSubscription>(pcs => pcs.ItemId == subscriptionData.ItemId, updates);

                _logger.LogInformation(
                    $"Made current subscription latest for {identifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update current subscription data for {Identifier}. Exception Message: {Message}. Exception Details: {StackTrace}.", identifier, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdatePreviousCustomSubscription(PraxisClient client, PraxisClientSubscription previousSubscription, UpdateCustomSubscriptionCommand command)
        {
            try
            {
                _logger.LogInformation("Update previous subscription for custom update with client id -> {ClientId}", client.ItemId);
                var subscriptionPackageInfo = _processClientDataService.GetSubscriptionPackageInfo(client);
                if (subscriptionPackageInfo == null)
                {
                    _logger.LogInformation("Subscription package seed not found for client id -> {ClientId}", client.ItemId);
                    return false;
                }
                previousSubscription.NumberOfUser = command.NumberOfUser;
                previousSubscription.DurationOfSubscription = command.DurationOfSubscription;
                previousSubscription.StorageSubscription.IncludedStorageInGigaBites = .5 * command.NumberOfUser;
                previousSubscription.StorageSubscription.TotalAdditionalStorageInGigaBites = command.AdditionalStorage;
                previousSubscription.SubscriptionDate = DateTime.UtcNow.ToLocalTime();
                previousSubscription.SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(command.DurationOfSubscription).AddDays(-1);
                previousSubscription.SubscriptionPackage = subscriptionPackageInfo.Title;
                previousSubscription.ModuleList = subscriptionPackageInfo.ModuleList;
                previousSubscription.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                await _repository.UpdateAsync(pcs => pcs.ItemId == previousSubscription.ItemId, previousSubscription);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in custom previous subscription update with error -> {ErrorMessage}", ex.Message);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdateClientNavigation(string clientId, List<NavInfo> navigationList, IEnumerable<NavigationDto> navigations)
        {
            try
            {
                var updateData = new
                {
                    Navigations = navigations
                };
                _repository.UpdateMany<PraxisClient>(r => r.ItemId == clientId, updateData);
                var navigationProcessService = _navigationPreparationTypeStrategy.GetServiceType("UPDATE");
                await navigationProcessService.ProcessNavigationData(clientId, navigationList);
                if (navigations.Any(nav => nav.Name == "PROCESS_GUIDE"))
                {
                    var praxisUser = _repository.GetItem<PraxisUser>(pu => pu.ClientList.Any(cl => cl.ClientId == clientId) && pu.Roles.Contains(RoleNames.PoweruserPayment + "_" + clientId));
                    foreach (var client in praxisUser.ClientList)
                    {
                        if (client.ClientId == clientId)
                        {
                            client.IsCreateProcessGuideEnabled = true;
                        }
                    }
                    await _repository.UpdateAsync<PraxisUser>(pu => pu.ItemId == praxisUser.ItemId, praxisUser);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in navigation update. Exception -> {ErrorMessage} for clientId -> {ClientId}", ex.Message, clientId);
            }
            return true;
        }

        private async Task<bool> UpdateUserCreatePermission(string clientId)
        {
            try
            {
                var praxisClientData = _repository.GetItem<PraxisClient>(x => x.ItemId == clientId);
                praxisClientData.IsCreateUserEnable = true;
                await _repository.UpdateAsync<PraxisClient>(x => x.ItemId == clientId, praxisClientData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in user create permission update for clientId -> {ClientId} - message -> {ErrorMessage}", clientId, ex.Message);
                return false;
            }
        }

        private void SaveClientSubscriptionNotificationData(PraxisClientSubscription subs)
        {
            try
            {
                _repository.Delete<PraxisClientSubscriptionNotification>(n => n.ClientId == subs.ClientId);

                var newPraxisClientSubscriptionNotification = new PraxisClientSubscriptionNotification
                {
                    ItemId = Guid.NewGuid().ToString(),
                    ClientId = subs.ClientId,
                    ClientEmail = subs.ClientEmail,
                    ExpirationRemainderDates = new List<DateTime>
                        {
                            DateTime.UtcNow.ToLocalTime().AddDays(90),
                            DateTime.UtcNow.ToLocalTime().AddDays(60),
                            DateTime.UtcNow.ToLocalTime().AddDays(30),
                            DateTime.UtcNow.ToLocalTime().AddDays(15)
                        },
                    SubscriptionExpirationDate = subs.SubscriptionExpirationDate,
                    DurationOfSubscription = Convert.ToString(subs.DurationOfSubscription)
                };
                _repository.Save(newPraxisClientSubscriptionNotification);
                _logger.LogInformation("Data has been successfully inserted to {EntityName} with ItemId: {ItemId}.", nameof(PraxisClientSubscriptionNotification), newPraxisClientSubscriptionNotification.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update client subscription notification data. Exception message: {ErrorMessage}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private async Task SendNotification(string subscriptionId, string context, string actionName, bool isSuccess)
        {
            var result = new
            {
                NotifiySubscriptionId = subscriptionId,
                Success = isSuccess
            };

            await _notificationService.PaymentNotification(isSuccess, subscriptionId, result, context, actionName);
        }

        private bool CheckClientSubscriptionTimeExpired(string clientId, string paymentDetailId)
        {
            var subscriptionStatus = string.Empty;
            if (paymentDetailId != null)
            {
                subscriptionStatus = _repository.GetItems<PraxisClientSubscription>(s => s.ClientId == clientId && s.PaymentHistoryId != paymentDetailId).OrderByDescending
               (o => o.CreateDate).Select(s => s.SubscritionStatus).FirstOrDefault();
            }
            else
            {
                subscriptionStatus = _repository.GetItems<PraxisClientSubscription>(s => s.ClientId == clientId && s.IsLatest).OrderByDescending
               (o => o.CreateDate).Select(s => s.SubscritionStatus).FirstOrDefault();
            }
            if (!string.IsNullOrEmpty(subscriptionStatus))
            {
                if (subscriptionStatus == nameof(PraxisEnums.EXPIRED))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private async Task<bool> SendInvoice(PraxisClientSubscription subscriptionData, string paymentDetailId)
        {
            try
            {
                var invoiceBillingAddress = new ClientBillingAddress
                {
                    Address = subscriptionData?.BillingAddress?.Address ?? string.Empty,
                    City = subscriptionData?.BillingAddress?.City ?? string.Empty,
                    CountryCode = subscriptionData?.BillingAddress?.CountryCode ?? string.Empty,
                    PostalCode = subscriptionData?.BillingAddress?.PostalCode ?? string.Empty,
                    Street = subscriptionData?.BillingAddress?.Street ?? string.Empty,
                    StreetNo = subscriptionData?.BillingAddress?.StreetNo ?? string.Empty
                };

                await _processPaymentInvoiceService.PrepareInvoiceData(invoiceBillingAddress, paymentDetailId);

                _logger.LogInformation("Send invoice successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in send invoice with error -> {ErrorMessage}", ex.Message);
                return false;
            }
        }

        private async Task SendEmailToSubscriptionUpdate(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var _translatedStringsAsDictionary = _uilmResourceKeyService
                .GetResourceValueByKeyName(ReportConstants.PaymentInvoiceTranslationsKeys, securityContext.Language);
                var latestPraxisClientSubscriptionInfo = _repository.GetItem<PraxisClientSubscription>(pcs => pcs.ClientId.Equals(clientId) && pcs.IsLatest);
                var previousPraxisClientSubscriptionInfo = _repository.GetItems<PraxisClientSubscription>(pcs =>
                                                            pcs.ClientId.Equals(clientId) && !pcs.IsLatest).OrderByDescending(pc => pc.CreateDate).FirstOrDefault();
                var subscribePraxisUser = _repository.GetItem<PraxisUser>(pu => pu.ClientList.Any(cl => cl.ClientId == clientId) && pu.Roles.Contains(RoleNames.PoweruserPayment + "_" + clientId));
                var newSubscriptionPlan = new SubscriptionPlan
                {
                    NumberOfUser = latestPraxisClientSubscriptionInfo.NumberOfUser,
                    DurationOfSubscription = latestPraxisClientSubscriptionInfo.DurationOfSubscription,
                    SubscriptionPackage = _translatedStringsAsDictionary[latestPraxisClientSubscriptionInfo.SubscriptionPackage]
                };

                if (previousPraxisClientSubscriptionInfo != null)
                {
                    var previousSubscriptionPlan = new SubscriptionPlan
                    {
                        NumberOfUser = previousPraxisClientSubscriptionInfo.NumberOfUser,
                        DurationOfSubscription = previousPraxisClientSubscriptionInfo.DurationOfSubscription,
                        SubscriptionPackage = _translatedStringsAsDictionary[previousPraxisClientSubscriptionInfo.SubscriptionPackage]
                    };
                    var emailData = _emailDataBuilder.BuildUserUserSubscriptionUpdateEmailData(newSubscriptionPlan, previousSubscriptionPlan, subscribePraxisUser);
                    await _emailNotifierService.SendUserSubscriptionUpdateEmail(subscribePraxisUser, emailData);
                }
            }
        }

        private async Task SendEmailToSubscriptionUpdateForOrg(
            string organizationId,
            PraxisClientSubscription currentSubscriptionData,
            PraxisClientSubscription previousSubscription)
        {
            if (!string.IsNullOrEmpty(organizationId))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var _translatedStringsAsDictionary =
                    _uilmResourceKeyService.GetResourceValueByKeyName(ReportConstants.PaymentInvoiceTranslationsKeys, securityContext.Language);

                var organization = GetOrganization(organizationId);

                var newSubscriptionPlan = new SubscriptionPlan
                {
                    NumberOfUser = currentSubscriptionData.NumberOfUser,
                    DurationOfSubscription = currentSubscriptionData.DurationOfSubscription,
                    SubscriptionPackage = _translatedStringsAsDictionary[currentSubscriptionData.SubscriptionPackage]
                };

                var previousSubscriptionPlan = new SubscriptionPlan
                {
                    NumberOfUser = previousSubscription.NumberOfUser,
                    DurationOfSubscription = previousSubscription.DurationOfSubscription,
                    SubscriptionPackage = _translatedStringsAsDictionary[previousSubscription.SubscriptionPackage]
                };

                var userIds = new string[] { organization.AdminUserId, organization.DeputyAdminUserId };
                var userList = _repository.GetItems<PraxisUser>(pu => userIds.Contains(pu.ItemId)).ToList();
                foreach(var user in userList )
                {
                    var emailData = _emailDataBuilder.BuildUserUserSubscriptionUpdateEmailData(newSubscriptionPlan, previousSubscriptionPlan, user);
                    await _emailNotifierService.SendUserSubscriptionUpdateEmail(user, emailData);
                }
            }
        }

        private async Task SendEmailToSubscriptionUpdateForClient(
           string clientId,
           PraxisClientSubscription currentSubscriptionData,
           PraxisClientSubscription previousSubscription)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var _translatedStringsAsDictionary =
                    _uilmResourceKeyService.GetResourceValueByKeyName(ReportConstants.PaymentInvoiceTranslationsKeys, securityContext.Language);

                var client = GetClient(clientId);
                var emailList = GetPraxisUserEmails(new string[] { client.AdminUserId, client.DeputyAdminUserId });

                var newSubscriptionPlan = new SubscriptionPlan
                {
                    //NumberOfUser = currentSubscriptionData.NumberOfUser,
                    DurationOfSubscription = currentSubscriptionData.DurationOfSubscription,
                    SubscriptionPackage = _translatedStringsAsDictionary[currentSubscriptionData.SubscriptionPackage]
                };

                var previousSubscriptionPlan = new SubscriptionPlan
                {
                    //NumberOfUser = previousSubscription.NumberOfUser,
                    DurationOfSubscription = previousSubscription.DurationOfSubscription,
                    SubscriptionPackage = _translatedStringsAsDictionary[previousSubscription.SubscriptionPackage]
                };

                var userIds = new string[] { client.AdminUserId, client.DeputyAdminUserId };
                var userList = _repository.GetItems<PraxisUser>(pu => userIds.Contains(pu.ItemId)).ToList();
                foreach (var user in userList)
                {
                    var emailData = _emailDataBuilder.BuildUserUserSubscriptionUpdateEmailData(newSubscriptionPlan, previousSubscriptionPlan, user);
                    await _emailNotifierService.SendUserSubscriptionUpdateEmail(user, emailData);
                }
            }
        }

        private async Task<bool> ProcessStorageLicensing(string clientId, double alreadyIncludedStorage, int storageLimit)
        {
            _logger.LogInformation("licensing start for client id -> {clientId}", clientId);
            var licenseData = PrepareUpdateLicensingPayload(clientId, alreadyIncludedStorage, storageLimit);
            var success = await _lincensingService.UpdateLicensingSpecification(licenseData);
            _logger.LogInformation("licensing for for client id -> {clientId} is success -> {success}", clientId, success);
            return success;
        }

        private UpdateLicensingSpecificationCommand PrepareUpdateLicensingPayload(string clientId, double alreadyIncludedStorage, int storageLimit)
        {
            var licenseData = new UpdateLicensingSpecificationCommand
            {
                FeatureId = "praxis-license",
                OrganizationId = clientId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = (((double)storageLimit + alreadyIncludedStorage) * 1024.0 * 1024.0 * 1024.0),
                CanOverUse = false,
                OverUseLimit = 0
            };
            return licenseData;
        }

        private async Task UpdateOrganizationUserLimit(PraxisClientSubscription praxisClientSubscription)
        {
            var updateData = new Dictionary<string, object>
                {
                    {"UserLimit",  praxisClientSubscription.NumberOfUser},
                    {"AuthorizedUserLimit", praxisClientSubscription.NumberOfUser*2},
                    {"AdditionalStorageLimit",  praxisClientSubscription?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0},
                    {"AdditionalLanguageTokenLimit",  praxisClientSubscription?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0},
                    {"AdditionalManualTokenLimit",  praxisClientSubscription?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0},

                };

            await _repository.UpdateAsync<PraxisOrganization>(po => po.ItemId == praxisClientSubscription.OrganizationId, updateData);
        }

        private List<string> GetPraxisUserEmails(string[] ids)
        {
            return _repository.GetItems<PraxisUser>(pu => ids.Contains(pu.ItemId))
                .Select(pu => pu.Email)
                .ToList();
        }

        private PraxisOrganization GetOrganization(string orgId)
        {
            return _repository.GetItem<PraxisOrganization>(o => o.ItemId == orgId);
        }

        private PraxisClient GetClient(string clientId)
        {
            return _repository.GetItem<PraxisClient>(o => o.ItemId == clientId);
        }

        // depricated
        public async Task UpdateSubscriptionInformation(UpdateClientSubscriptionInformationCommand command)
        {
            _logger.LogInformation("Going to update client subscription information.");
            try
            {
                var existingClientSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.PaymentHistoryId == command.PaymentDetailId);

                if (existingClientSubscription != null)
                {
                    if (existingClientSubscription.IsLatest)
                    {
                        _logger.LogInformation("subscription info already updated with clientId -> {ClientId}", existingClientSubscription.ClientId);
                        return;
                    }
                    _logger.LogInformation("going to update subscription info with clientId -> {ClientId}", existingClientSubscription.ClientId);
                    var previousSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.ClientId == existingClientSubscription.ClientId && s.IsLatest);
                    await UpdatePreviousSubscription(previousSubscription);
                    existingClientSubscription.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    existingClientSubscription.IsLatest = true;
                    await _repository.UpdateAsync<PraxisClientSubscription>(pm => pm.ItemId == existingClientSubscription.ItemId, existingClientSubscription);
                    _logger.LogInformation("Data has been successfully updated to {EntityName} entity with ItemId: {ItemId}", nameof(PraxisClientSubscription), existingClientSubscription.ItemId);
                    if (previousSubscription.StorageSubscription?.TotalAdditionalStorageInGigaBites !=
                        existingClientSubscription.StorageSubscription?.TotalAdditionalStorageInGigaBites ||
                        previousSubscription.NumberOfUser != existingClientSubscription.NumberOfUser)
                    {
                        var alreadyIncludedStorage = (.5 * existingClientSubscription.NumberOfUser);
                        await ProcessStorageLicensing(existingClientSubscription.ClientId, alreadyIncludedStorage, (int)existingClientSubscription.StorageSubscription.TotalAdditionalStorageInGigaBites);
                    }
                    if (previousSubscription.SubscriptionPackage != existingClientSubscription.SubscriptionPackage)
                    {
                        await UpdateClientNavigation(command.ClientId, command.NavigationList, command.Navigations);
                    }
                    SaveClientSubscriptionNotificationData(existingClientSubscription);
                    if (existingClientSubscription.NumberOfUser > previousSubscription.NumberOfUser)
                    {
                        await UpdateUserCreatePermission(existingClientSubscription.ClientId);
                    }
                    if (CheckClientSubscriptionTimeExpired(command.ClientId, command.PaymentDetailId))
                    {
                        await _activateUserAccountService.ActivateAccount(command.ClientId);
                    }
                    await SendInvoice(existingClientSubscription, command.PaymentDetailId);
                    await SendEmailToSubscriptionUpdate(command.ClientId);
                    await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with PaymentHistoryId: {PaymentDetailId}.", nameof(PraxisClientSubscription), command.PaymentDetailId);
                    await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update client subscription information. Exception message: {ErrorMessage}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
                await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
            }
        }

        public async Task<bool> UpdateCustomSubscriptionInformation(UpdateCustomSubscriptionCommand command)
        {
            try
            {
                var clientData = _repository.GetItem<PraxisClient>(pxc => pxc.ItemId == command.ClientId);
                if (clientData != null)
                {
                    var previousSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.ClientId == command.ClientId && s.IsLatest);
                    var previousAdditionalStorage = previousSubscription.StorageSubscription.TotalAdditionalStorageInGigaBites;
                    var previousNumberOfUser = previousSubscription.NumberOfUser;
                    var isSuccess = false;
                    if (previousSubscription.PaymentMethod.Equals("Cash"))
                    {
                        isSuccess = await UpdatePreviousCustomSubscription(clientData, previousSubscription, command);
                    }
                    else
                    {
                        await UpdatePreviousSubscription(previousSubscription);
                        isSuccess = _praxisClientCustomSubscriptionService.SaveSubscriptionData(clientData, command.NumberOfUser, command.DurationOfSubscription
                        , command.PaymentMethod, command.AdditionalStorage);
                    }
                    if (isSuccess)
                    {
                        _logger.LogInformation("Custom subscription licensing data -> previous storage: {PreviousStorage}, new storage: {NewStorage}", previousAdditionalStorage, command.AdditionalStorage);

                        if (previousAdditionalStorage != command.AdditionalStorage || previousNumberOfUser != command.NumberOfUser)
                        {
                            var alreadyIncludedStorage = (.5 * (command.NumberOfUser));
                            await ProcessStorageLicensing(command.ClientId, alreadyIncludedStorage, command.AdditionalStorage);
                        }
                        // SaveClientSubscriptionNotificationData(command.ClientId, clientData.ContactEmail, command.DurationOfSubscription);
                        if (command.NumberOfUser > previousSubscription.NumberOfUser)
                        {
                            await UpdateUserCreatePermission(command.ClientId);
                        }
                        if (CheckClientSubscriptionTimeExpired(command.ClientId, null))
                        {
                            await _activateUserAccountService.ActivateAccount(command.ClientId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Custom update subscription info failed");
                    }
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with clientId: {ClientId}.", nameof(PraxisClient), command.ClientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update custom subscription with error -> {ErrorMessage}", ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> RemoveCustomSubscriptionInformation(RemoveCustomSubscriptionCommand command)
        {
            try
            {
                var clientData = _repository.GetItem<PraxisClient>(pxc => pxc.ItemId == command.ClientId);
                if (clientData != null)
                {
                    var previousSubscription = _repository.GetItem<PraxisClientSubscription>(s => s.ClientId == command.ClientId && s.IsLatest);
                    var previousAdditionalStorage = previousSubscription.StorageSubscription.TotalAdditionalStorageInGigaBites;

                    var isUpdated = await UpdatePreviousSubscription(previousSubscription);

                    if (isUpdated)
                    {
                        _logger.LogInformation("Custom subscription licensing data -> previous storage: {PreviousStorage}, new storage: {NewStorage}", previousAdditionalStorage, command.AdditionalStorage);

                        var alreadyIncludedStorage = (.5 * (command.NumberOfUser));
                        await ProcessStorageLicensing(command.ClientId, alreadyIncludedStorage, command.AdditionalStorage);

                        await UpdateUserCreatePermission(command.ClientId);

                    }
                    else
                    {
                        _logger.LogInformation("Custom remove subscription info failed");
                    }
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with clientId: {ClientId}.", nameof(PraxisClient), command.ClientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update custom subscription with error -> {ErrorMessage}", ex.Message);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdateDepartmentSubscription(PraxisClientSubscription currentSubscription)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentSubscription?.ClientId))
                {
                    await _departmentSubscriptionService.SaveDepartmentSubscription(currentSubscription.ClientId, currentSubscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update previous department subscription data for clientId: {ClientId}. Exception Message: {Message}. Exception Details: {StackTrace}.", currentSubscription.ClientId, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<bool> UpdateOrganizationSubscription(OrganizationSubscription organizationSubscription)
        {
            try
            {
                if (!string.IsNullOrEmpty(organizationSubscription.OrganizationId))
                {
                    await _organizationSubscriptionService.SaveOrganizationSubscription(organizationSubscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in update previous organization subscription data for organizationId: {OrganizationId}. Exception Message: {Message}. Exception Details: {StackTrace}.", organizationSubscription.OrganizationId, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        public async Task ProcessOfflineUpdateSubscriptionAfterEffects(UpdateClientSubscriptionInformationCommand command)
        {
            _logger.LogInformation("Going to update subscription data for organizationId -> {OrganizationId}", command.OrganizationId);
            try
            {
                var currentSubscriptionData = await _praxisClientSubscriptionService.GetSubscriptionDataByPaymentDetailId(command.PaymentDetailId);

                if (currentSubscriptionData != null)
                {
                    if (currentSubscriptionData.IsLatest)
                    {
                        _logger.LogInformation("Going to update subscription data for organizationId -> {OrganizationId}", command.OrganizationId);
                        return;
                    }

                    var previousSubscription = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(currentSubscriptionData.OrganizationId);

                    if (currentSubscriptionData.IsActive)
                    {
                        var isSubscriptionDataUpdateSucccess = await UpdatePreviousSubscription(previousSubscription);
                        isSubscriptionDataUpdateSucccess &= await UpdateCurrentSubscription(currentSubscriptionData, previousSubscription);
                        if (isSubscriptionDataUpdateSucccess)
                        {
                            await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, true);
                        }

                        await ProcessSubscriptionInfoAsync(currentSubscriptionData, currentSubscriptionData.OrganizationId);
                    }
                }
                else
                {
                    _logger.LogInformation("No {EntityName} entity data found with PaymentHistoryId: {PaymentDetailId}.", nameof(PraxisClientSubscription), command.PaymentDetailId);
                    await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update client subscription information. Exception message: {ErrorMessage}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
                await SendNotification(command.NotificationSubscriptionId, command.Context, command.ActionName, false);
            }
        }
    }
}
