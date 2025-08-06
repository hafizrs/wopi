using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Subscriptions.PaymentModule;
using static Aspose.Pdf.CollectionItem;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisClientSubscriptionService : IPraxisClientSubscriptionService
    {
        private readonly ILogger<PraxisClientSubscriptionService> _logger;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISubscriptionPricingCustomPackageService _subscriptionPricingCustomPackageService;
        private readonly ISubscriptionCalculationService _subscriptionEstimatedBillCalculationService;

        public PraxisClientSubscriptionService(
            ILogger<PraxisClientSubscriptionService> logger,
            IRepository repository,
            IChangeLogService changeLogService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            ICommonUtilService commonUtilService,
            ISubscriptionPricingCustomPackageService subscriptionPricingCustomPackageService,
            ISubscriptionCalculationService subscriptionEstimatedBillCalculationService)
        {
            _logger = logger;
            _repository = repository;
            _changeLogService = changeLogService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _commonUtilService = commonUtilService;
            _subscriptionPricingCustomPackageService = subscriptionPricingCustomPackageService;
            _subscriptionEstimatedBillCalculationService = subscriptionEstimatedBillCalculationService;
        }

        public async Task<PraxisClientSubscription> GetSubscriptionDataByPaymentDetailId(string paymentDetailId)
        {
            return await _repository.GetItemAsync<PraxisClientSubscription>(x => x.PaymentHistoryId == paymentDetailId);
        }

        public async Task<PraxisClientSubscription> GetOrganizationLatestSubscriptionData(string organizationId)
        {
            return await _repository.GetItemAsync<PraxisClientSubscription>(pcs => pcs.OrganizationId == organizationId && pcs.IsActive && pcs.IsLatest);
        }

        public async Task<PraxisClientSubscription> GetClientLatestSubscriptionData(string clientId)
        {
            return await _repository.GetItemAsync<PraxisClientSubscription>(pcs => pcs.ClientId == clientId && pcs.IsActive && pcs.IsLatest);
        }

        public async Task SaveSubscriptionRelatedDataOnPurchase(
            PraxisOrganization organizationData,
            string paymentDetailId,
            string adminEmail,
            string deputyAdminEmail,
            ClientBillingAddress billingAddress,
            ResponsiblePerson responsiblePerson)
        {
            var subscriptionData = await GetSubscriptionDataByPaymentDetailId(paymentDetailId);

            if (subscriptionData != null)
            {
                await UpdateSubscriptionDataOnPurchase(
                    organizationData,
                    subscriptionData,
                    adminEmail,
                    deputyAdminEmail,
                    billingAddress,
                    responsiblePerson);

                await SaveSubscriptionNotification(organizationData.ItemId, subscriptionData);
                await _subscriptionPricingCustomPackageService.UpdateSubscriptionUsageStatus(subscriptionData.ItemId);
            }
        }

        public DateTime GetSubcriptionStartDateTime(DateTime subscriptionStartDate)
        {
            var parsedSubscriptionStartDate = DateTime.Parse(subscriptionStartDate.ToString());
            var startDate = new DateTime(parsedSubscriptionStartDate.Year, parsedSubscriptionStartDate.Month, parsedSubscriptionStartDate.Day, 0, 0, 0, 0);
            return startDate.ToUniversalTime().ToLocalTime();
        }

        public DateTime GetSubcriptionExpiryDateTime(DateTime subscriptionStartDate, int subscriptionDuration)
        {
            var parsedSubscriptionStartDate = DateTime.Parse(subscriptionStartDate.ToString());
            var date = parsedSubscriptionStartDate.AddMonths(subscriptionDuration);
            var expiryDate = date.Date.AddSeconds(-1);
            return expiryDate;
        }

        public DateTime GetSubcriptionRenewalStartDateTime(DateTime subscriptionStartDate, int subscriptionDuration)
        {
            var date = subscriptionStartDate.AddMonths(subscriptionDuration);
            var expiryDate = date.Date;
            return expiryDate;
        }

        private async Task UpdateSubscriptionDataOnPurchase(
            PraxisOrganization organizationData,
            PraxisClientSubscription subscriptionData,
            string adminEmail,
            string deputyAdminEmail,
            ClientBillingAddress billingAddress,
            ResponsiblePerson responsiblePerson)
        {
            try

            {
                var clientBillingbillingAddress = new ClientBillingAddress
                {
                    Address = billingAddress.Address,
                    Street = billingAddress.Street,
                    StreetNo = billingAddress.StreetNo,
                    PostalCode = billingAddress.PostalCode,
                    City = billingAddress.City,
                    CountryCode = billingAddress.CountryCode
                };

                var billingResponsiblePersonInfo = new ClientResponsiblePerson
                {
                    Email = responsiblePerson.Email,
                    FirstName = responsiblePerson.FirstName,
                    LastName = responsiblePerson.LastName,
                    Phone = responsiblePerson.Phone
                };

                var necessaryRoles = new List<string>() { RoleNames.Admin, RoleNames.AdminB }.ToArray();

                var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"PaidAmount", subscriptionData.GrandTotal},
                        {"SubscritionStatus", nameof(PraxisEnums.ONGOING) },
                        {"OrganizationId", organizationData.ItemId},
                        {"OrganizationName", organizationData.ClientName},
                        {"BillingAddress", clientBillingbillingAddress},
                        {"ResponsiblePerson", billingResponsiblePersonInfo},
                        {"RolesAllowedToRead", necessaryRoles },
                        {"IdsAllowedToRead",  PrepareSubscriptionDataReadPermission( new[] { adminEmail, deputyAdminEmail })}
                    };

                var filterBuilder = Builders<BsonDocument>.Filter;
                var updateFilters = filterBuilder.Eq("_id", subscriptionData.ItemId);

                await _changeLogService.UpdateChange(nameof(PraxisClientSubscription), updateFilters, updates);

                _logger.LogInformation("Updated current subscription data after successful purchase for organizationId: {OrganizationId}", subscriptionData.OrganizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during current subscription data update for organizationId: {OrganizationId}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.",
                    subscriptionData.OrganizationId, ex.Message, ex.StackTrace);
            }
        }

        public async Task<bool> SaveSubscriptionNotification(string organizationId, PraxisClientSubscription subscriptionData, bool isAPurchase = true)
        {
            try
            {
                var expiredDate = subscriptionData.SubscriptionExpirationDate;
                var subscriptionNotification = new PraxisClientSubscriptionNotification
                {
                    ItemId = Guid.NewGuid().ToString(),
                    OrganizationId = organizationId,
                    OrganizationEmail = subscriptionData.OrganizationEmail,
                    SubscriptionExpirationDate = expiredDate,
                    ExpirationRemainderDates = subscriptionData.DurationOfSubscription == 12 ? new List<DateTime>
                    {
                        expiredDate.AddDays(-90),
                        expiredDate.AddDays(-60),
                        expiredDate.AddDays(-30),
                        expiredDate.AddDays(-15)
                    } :
                    new List<DateTime>
                    {
                        expiredDate.AddDays(-14)
                    },
                    DurationOfSubscription = Convert.ToString(subscriptionData.DurationOfSubscription),
                    IsActive = isAPurchase
                };

                await _repository.SaveAsync(subscriptionNotification);
                _logger.LogInformation("Data has been successfully inserted to {EntityName} with ItemId: {ItemId}.", nameof(PraxisClientSubscriptionNotification), subscriptionNotification.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in process subscription notification with error: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        public async Task<bool> SaveSubscriptionNotificationForClient(string clientId, PraxisClientSubscription subscriptionData, bool isAPurchase = true)
        {
            try
            {
                var expiredDate = subscriptionData.SubscriptionExpirationDate;
                var subscriptionNotification = new PraxisClientSubscriptionNotification
                {
                    ItemId = Guid.NewGuid().ToString(),
                    ClientId = clientId,
                    ClientEmail = subscriptionData.ClientEmail,
                    SubscriptionExpirationDate = expiredDate,
                    ExpirationRemainderDates = subscriptionData.DurationOfSubscription == 12 ? new List<DateTime>
                    {
                        expiredDate.AddDays(-90),
                        expiredDate.AddDays(-60),
                        expiredDate.AddDays(-30),
                        expiredDate.AddDays(-15)
                    } :
                    new List<DateTime>
                    {
                        expiredDate.AddDays(-14)
                    },
                    DurationOfSubscription = Convert.ToString(subscriptionData.DurationOfSubscription),
                    IsActive = isAPurchase
                };

                await _repository.SaveAsync(subscriptionNotification);
                _logger.LogInformation(
                    $"Data has been successfully inserted to {nameof(PraxisClientSubscriptionNotification)} " +
                    $"with ItemId: {subscriptionNotification.ItemId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in process subscriprion notification with error -> {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task SaveClientSubscriptionOnClientCreateUpdate(string clientId)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId)) return;

                var client = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == clientId);
                var orgSubscription = await GetOrganizationLatestSubscriptionData(client?.ParentOrganizationId);
                if (orgSubscription == null)
                {
                    _logger.LogInformation("No Org Subscription Found");
                    return;
                }

                var clientSubscription = await GetClientLatestSubscriptionData(clientId);

                if (clientSubscription == null)
                {
                    var rolesAllowToRead = new List<string>()
                    {
                        $"{RoleNames.PowerUser_Dynamic}_{client.ItemId}",
                        $"{RoleNames.Leitung_Dynamic}_{client.ItemId}",
                        $"{RoleNames.MpaGroup_Dynamic}_{client.ItemId}",
                        $"{RoleNames.Admin}",
                        $"{RoleNames.TaskController}"
                    };
                    clientSubscription = new PraxisClientSubscription
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        RolesAllowedToRead = rolesAllowToRead.ToArray(),
                        NumberOfUser = client.UserLimit,
                        NumberOfAuthorizedUsers = client.UserLimit * 2,
                        DurationOfSubscription = orgSubscription.DurationOfSubscription,
                        SubscriptionPackage = orgSubscription.SubscriptionPackage,
                        ModuleList = orgSubscription.ModuleList,
                        ClientEmail = client.ContactEmail,
                        SubscriptionDate = orgSubscription.SubscriptionDate,
                        PerUserCost = orgSubscription?.PerUserCost ?? 0,
                        AverageCost = orgSubscription?.AverageCost ?? 0,
                        SubscriptionExpirationDate = orgSubscription.SubscriptionExpirationDate,
                        IsOrgTypeChangeable = orgSubscription.IsOrgTypeChangeable,
                        SubscritionStatus = nameof(PraxisEnums.ONGOING),
                        PaymentMethod = "Annually",
                        PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                        PaidAmount = 0,
                        GrandTotal = 0,
                        IsLatest = true,
                        IsActive = true,
                        IsMarkedToDelete = false,
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        ClientId = client.ItemId,
                        ClientName = client.ClientName,
                        OrganizationType = orgSubscription.OrganizationType,
                        Location = orgSubscription.Location,
                        PaymentCurrency = orgSubscription.PaymentCurrency,
                        StorageSubscription = new StorageSubscriptionInfo
                        {
                            IncludedStorageInGigaBites = client.UserLimit * .5,
                            TotalAdditionalStorageInGigaBites = 0,
                            TotalAdditionalStorageCost = 0
                        },
                        TokenSubscription = new TokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = 0,
                            TotalAdditionalTokenInMillion = 0,
                            TotalAdditionalTokenCost = 0
                        },
                        ManualTokenSubscription = new ManualTokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = 0,
                            TotalAdditionalTokenInMillion = 0,
                            TotalAdditionalTokenCost = 0
                        },
                        TotalTokenSubscription = new TotalTokenSubscriptionInfo
                        {
                            TotalTokenInMillion = 0,
                            TotalTokenCost = 0
                        },
                        Tags = new string[] { "Is-Valid-PraxisClient" },
                        PaymentMode = "Annually"
                    };

                    await _repository.SaveAsync(clientSubscription);

                    await SaveSubscriptionNotificationForClient(clientId, clientSubscription);

                    _logger.LogInformation(
                        $"Data has been successfully inserted to {nameof(PraxisClientSubscriptionNotification)} " +
                        $"with ItemId: {clientSubscription.ItemId}.");
                } 
                else
                {
                    clientSubscription.ClientName = client.ClientName;
                    clientSubscription.ClientEmail = client.ContactEmail;
                    clientSubscription.NumberOfUser = client.UserLimit;
                    clientSubscription.NumberOfAuthorizedUsers = client.UserLimit * 2;
                    clientSubscription.StorageSubscription ??= new StorageSubscriptionInfo();
                    clientSubscription.TokenSubscription ??= new TokenSubscriptionInfo();
                    clientSubscription.ManualTokenSubscription ??= new ManualTokenSubscriptionInfo();
                    clientSubscription.TotalTokenSubscription ??= new TotalTokenSubscriptionInfo();
                    clientSubscription.StorageSubscription.IncludedStorageInGigaBites = client.UserLimit * 0.5;

                    await _repository.UpdateAsync(s => s.ItemId == clientSubscription.ItemId, clientSubscription);
                }
                await _departmentSubscriptionService.SaveDepartmentSubscription(clientId, clientSubscription);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in SaveClientSubscriptionOnClientCreateUpdate -> {ex.Message}");
            }
        }

        public async Task SaveClientSubscriptionOnOrgCreateUpdate(string orgId, PraxisClientSubscription subs = null, PraxisOrganization orgData = null)
        {
            try
            {
                if (string.IsNullOrEmpty(orgId)) return;

                PraxisClientSubscription orgSubscription = null;

                if (orgData == null)
                {
                    orgData = await _repository.GetItemAsync<PraxisOrganization>(c => c.ItemId == orgId);

                    if (orgData == null)
                    {
                        _logger.LogInformation("No Org Data Found");
                        return;
                    }

                    orgSubscription = await GetOrganizationLatestSubscriptionData(orgId);
                }

                if (orgSubscription == null)
                {
                    var subcriptionPackageInfo = _repository.GetItem<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId)?
                                                    .SubscriptionPackages?.FirstOrDefault(x => x.Title == PraxisPaymentConstants.CompletePackage);
                    var necessaryIds = (!string.IsNullOrEmpty(orgData.AdminUserId) || !string.IsNullOrEmpty(orgData.DeputyAdminUserId)) ? 
                                _repository.GetItems<PraxisUser>
                                    (pu => pu.ItemId == orgData.AdminUserId || pu.ItemId == orgData.DeputyAdminUserId)?
                                    .Select(pu => pu.UserId)?.ToArray() : new string[] {};

                    var necessaryRoles = new List<string>() { RoleNames.Admin, RoleNames.AdminB }.ToArray();

                    orgSubscription = new PraxisClientSubscription
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        RolesAllowedToRead = necessaryRoles,
                        IdsAllowedToRead = necessaryIds,
                        NumberOfUser = orgData.UserLimit,
                        NumberOfAuthorizedUsers = orgData.UserLimit * 2,
                        DurationOfSubscription = subs?.DurationOfSubscription ?? 12,
                        SubscriptionPackage = subcriptionPackageInfo.Title,
                        ModuleList = subcriptionPackageInfo.ModuleList,
                        OrganizationId = orgData.ItemId,
                        OrganizationName = orgData.ClientName,
                        OrganizationEmail = orgData.ContactEmail,
                        SubscriptionDate = orgData.CreateDate.Year > 1000 ? orgData.CreateDate : DateTime.UtcNow,
                        SubscriptionExpirationDate = GetSubcriptionExpiryDateTime(orgData.CreateDate.Year > 1000 ? orgData.CreateDate : DateTime.UtcNow, subs?.DurationOfSubscription ?? 12),
                        IsOrgTypeChangeable = true,
                        SubscritionStatus = nameof(PraxisEnums.ONGOING),
                        PaymentMethod = subs?.PaymentMethod ?? "NONE",
                        BillingAddress = subs?.BillingAddress,
                        ResponsiblePerson = subs?.ResponsiblePerson,
                        PaymentInvoiceId = "P-" + _commonUtilService.GenerateRandomInvoiceId(),
                        LastUpdateDate = DateTime.UtcNow,
                        OrganizationType = PraxisPaymentConstants.OrganizationTypeId,
                        Location = subs?.Location ?? "CH",
                        PerUserCost = subs?.PerUserCost ?? 0,
                        AverageCost = subs?.AverageCost ?? 0,
                        TaxDeduction = subs?.TaxDeduction ?? 0,
                        GrandTotal = subs?.GrandTotal ?? 0,
                        PaymentCurrency = subs?.PaymentCurrency ?? "chf",
                        PaymentHistoryId = Guid.NewGuid().ToString(),
                        PaidAmount = subs?.PaidAmount ?? 0,
                        IsTokenApplied = subs?.IsTokenApplied ?? false,
                        IsActive = true,
                        IsLatest = true,
                        StorageSubscription = new StorageSubscriptionInfo
                        {
                            IncludedStorageInGigaBites = orgData.UserLimit * .5,
                            TotalAdditionalStorageInGigaBites = subs?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0.0,
                            TotalAdditionalStorageCost = subs?.StorageSubscription?.TotalAdditionalStorageCost ?? 0.0
                        },
                        TokenSubscription = new TokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = subs?.TokenSubscription?.IncludedTokenInMillion ?? 0.0,
                            TotalAdditionalTokenInMillion = subs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                            TotalAdditionalTokenCost = subs?.TokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                        },
                        ManualTokenSubscription = new ManualTokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = subs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0.0,
                            TotalAdditionalTokenInMillion = subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                            TotalAdditionalTokenCost = subs?.ManualTokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                        },
                        TotalTokenSubscription = new TotalTokenSubscriptionInfo
                        {
                            TotalTokenInMillion = subs?.TotalTokenSubscription?.TotalTokenInMillion ?? 0.0,
                            TotalTokenCost = subs?.TotalTokenSubscription?.TotalTokenCost ?? 0.0,
                        },
                        SubscriptionInstallments = subs?.SubscriptionInstallments ?? new List<SubscriptionInstallment>(),
                        TotalPerMonthDueCosts = subs?.TotalPerMonthDueCosts ?? new List<PraxisKeyValue>(),
                        TaxPercentage = subs?.TaxPercentage ?? 0.0,
                        IsManualTokenApplied = subs?.IsManualTokenApplied ?? false,
                        Tags = new string[] { "Is-Valid-PraxisClient" },
                        PaymentMode = "OFFLINE",
                        InvoiceMetaData = PrepareInvoiceMetaData(subs)
                    };

                    await _repository.SaveAsync(orgSubscription);

                    await SaveSubscriptionNotification(orgId, orgSubscription);

                    _logger.LogInformation(
                        $"Data has been successfully inserted to {nameof(PraxisClientSubscriptionNotification)} for org" +
                        $"with ItemId: {orgSubscription.ItemId}.");
                }
                else
                {
                    orgSubscription.OrganizationName = orgData.ClientName;
                    orgSubscription.OrganizationEmail = orgData.ContactEmail;
                    orgSubscription.NumberOfUser = subs.NumberOfUser;
                    orgSubscription.NumberOfAuthorizedUsers = subs.NumberOfUser * 2;
                    orgSubscription.GrandTotal = subs?.GrandTotal ?? 0;
                    orgSubscription.PaidAmount = subs?.PaidAmount ?? 0;
                    orgSubscription.InvoiceMetaData = PrepareInvoiceMetaData(subs);

                    await _repository.UpdateAsync(s => s.ItemId == orgSubscription.ItemId, orgSubscription);

                    orgData.UserLimit = subs.NumberOfUser;
                    orgData.AuthorizedUserLimit = subs.NumberOfUser * 2;

                    await _repository.UpdateAsync(s => s.ItemId == orgData.ItemId, orgData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in SaveClientSubscriptionOnClientCreateUpdate -> {ex.Message}");
            }
        }

        private List<PraxisKeyValue> PrepareInvoiceMetaData(PraxisClientSubscription command)
        {
            var invoiceMetaData = new List<PraxisKeyValue>();

            AddKeyValue("NumberOfUser", (command?.NumberOfUser ?? 0).ToString("F2"));
            AddKeyValue("AdditionalStorage", (command?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0).ToString("F2"));
            AddKeyValue("AdditionalLanguageToken", (command?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("AdditionalManualToken", (command?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0).ToString("F2"));
            AddKeyValue("TaxDeduction", (command?.TaxDeduction ?? 0).ToString("F2"));
            AddKeyValue("DurationOfSubscription", (command?.DurationOfSubscription ?? 12).ToString("F2"));
            AddKeyValue("PerUserCost", (command?.PerUserCost ?? 0).ToString("F2"));
            AddKeyValue("AverageCost", (command?.AverageCost ?? 0).ToString("F2"));
            AddKeyValue("PaymentMethod", "Annually");
            AddKeyValue("InvoiceType", ((int)SubscriptionInvoiceType.NewOrRenew).ToString());
            AddKeyValue("IsOfflineInvoice", ("True"));
            AddKeyValue("MarkAsPaid", ("False"));
            return invoiceMetaData;

            void AddKeyValue(string key, string value)
            {
                invoiceMetaData.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

        public async Task UpdateExpiredSubscriptionData(string excludeId, string organizationId, string clientId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsLatest", false },
                    { "IsActive", false },
                    { "SubscritionStatus", nameof(PraxisEnums.EXPIRED) },
                    { "LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                };

                if (!string.IsNullOrEmpty(organizationId))
                {
                    await _repository.UpdateManyAsync<PraxisClientSubscription>(s => s.ItemId != excludeId && s.OrganizationId == organizationId && s.IsActive, updates);

                    _logger.LogInformation("Updated expired subscription data for organizationId: {OrganizationId}", organizationId);
                }
                else if (!string.IsNullOrEmpty(clientId))
                {
                    await _repository.UpdateManyAsync<PraxisClientSubscription>(s => s.ItemId != excludeId && s.ClientId == clientId && s.IsActive, updates);

                    _logger.LogInformation("Updated expired subscription data for clientId: {ClientId}", clientId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during expired subscription data update. " +
                    $"Exception Message: {ex.Message}. " +
                    $"Exception Details: {ex.StackTrace}.");
            }
        }

        public async Task UpdateSubscriptionRenewalData(PraxisClientSubscription subscriptionData)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsLatest", true },
                    { "IsActive", true },
                    { "SubscritionStatus", nameof(PraxisEnums.ONGOING) },
                    { "LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                };

                await _repository.UpdateAsync<PraxisClientSubscription>(pcs => pcs.ItemId == subscriptionData.ItemId, updates);

                _logger.LogInformation("Updated subscription renewal data for ItemId: {ItemId}", subscriptionData.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during subscription renewal data update for organizationId: {subscriptionData.OrganizationId}. " +
                    $"Exception Message: {ex.Message}. " +
                    $"Exception Details: {ex.StackTrace}.");
            }
        }

        public async Task<bool> UpdateExpiredSubscriptionNotificationData(string organizationId, string clientId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsActive", false },
                    { "LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                };
                
                if (!string.IsNullOrEmpty(organizationId))
                {
                    await _repository.UpdateManyAsync<PraxisClientSubscriptionNotification>(n => n.OrganizationId == organizationId, updates);

                    _logger.LogInformation("Updated expired subscription notification data for organizationId: {OrganizationId}", organizationId);
                }
                else if (!string.IsNullOrEmpty(clientId))
                {
                    await _repository.UpdateManyAsync<PraxisClientSubscriptionNotification>(n => n.ClientId == clientId, updates);

                    _logger.LogInformation("Updated expired subscription notification data for clientId: {ClientId}", clientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during expired subscription notification data update. " +
                    $"Exception Message: {ex.Message}. " +
                    $"Exception Details: {ex.StackTrace}.");

                return false;
            }
            return true;
        }

        public async Task<bool> UpdateSubscriptionRenewalNotificationData(string notificationId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsActive", true },
                    { "LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                };

                await _repository.UpdateAsync<PraxisClientSubscriptionNotification>(n => n.ItemId == notificationId, updates);

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during subscription renewal notification data update" +
                    $"Exception Message: {ex.Message}. " +
                    $"Exception Details: {ex.StackTrace}.");

                return false;
            }
            return true;
        }

        private string[] PrepareSubscriptionDataReadPermission(string[] emails)
        {
            return _repository.GetItems<PraxisUser>(pu => emails.Contains(pu.Email) && !pu.IsMarkedToDelete)
                .Select(pu => pu.UserId)
                .ToArray();
        }

        public async Task UpdateSubscriptionInvoicePdfFileId(string subscriptionId, string invoicePdfFileId)
        {
            try
            {
                if (!string.IsNullOrEmpty(invoicePdfFileId))
                {
                    var existingData = await _repository.GetItemAsync<PraxisClientSubscription>(x => x.ItemId == subscriptionId);

                    if (existingData != null)
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "InvoicePdfFileId", invoicePdfFileId }
                        };
                        await _repository.UpdateAsync<PraxisClientSubscription>(cs => cs.ItemId.Equals(existingData.ItemId), updates);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public async Task MarkAsPaidOfflineInvoice(MarkAsPaidOfflineInvoiceComman command)
        {
            try
            {
                if (string.IsNullOrEmpty(command.OrganizationId) || string.IsNullOrEmpty(command.PaymentHistoryId) || !command.MarkAsPaid)
                {
                    _logger.LogWarning("Invalid command input: OrganizationId or PaymentHistoryId is missing, or MarkAsPaid is false.");
                    return;
                }

                var invoice = await _repository.GetItemAsync<PraxisClientSubscription>(
                    p => p.OrganizationId == command.OrganizationId && p.PaymentHistoryId == command.PaymentHistoryId);

                if (invoice == null)
                {
                    _logger.LogWarning("No invoice found for OrganizationId: {OrganizationId}, PaymentHistoryId: {PaymentHistoryId}",
                        command.OrganizationId, command.PaymentHistoryId);
                    return;
                }

                var paymentDetails = new MarkAsPaidDetails
                {
                    PaymentDate = command.PaymentDate,
                    Remarks = command.Remarks,
                    ImageFileId = command.ImageFileId
                };

                string serializedDetails = JsonConvert.SerializeObject(paymentDetails);

                AddOrUpdateMetaData(invoice.InvoiceMetaData, "MarkAsPaid", "True");
                AddOrUpdateMetaData(invoice.InvoiceMetaData, "MarkAsPaidDetails", serializedDetails);

                await _repository.UpdateAsync<PraxisClientSubscription>(c => c.ItemId == invoice.ItemId, invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {MethodName}", nameof(MarkAsPaidOfflineInvoice));
            }
        }

        private void AddOrUpdateMetaData(List<PraxisKeyValue> metaDataList, string key, string value)
        {
            var item = metaDataList.FirstOrDefault(kv => kv.Key == key);
            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                metaDataList.Add(new PraxisKeyValue { Key = key, Value = value });
            }
        }

    }
}
