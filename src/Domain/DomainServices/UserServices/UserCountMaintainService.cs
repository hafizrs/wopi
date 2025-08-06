using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using MongoDB.Driver;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class UserCountMaintainService : IUserCountMaintainService
    {
        private readonly ILogger<UserCountMaintainService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider ecapRepository;
        private readonly IEmailDataBuilder _emailDataBuilder;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IChangeLogService _changeLogService;

        public UserCountMaintainService(
            ILogger<UserCountMaintainService> logger,
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IUilmResourceKeyService uilmResourceKeyService,
            IEmailDataBuilder emailDataBuilder,
            IEmailNotifierService emailNotifierService,
            IChangeLogService changeLogService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _repository = repository;
            this.ecapRepository = ecapRepository;
            _uilmResourceKeyService = uilmResourceKeyService;
            _emailDataBuilder = emailDataBuilder;
            _emailNotifierService = emailNotifierService;
            _changeLogService = changeLogService;
        }

        public async Task InitiateUserCountUpdateProcessOnUserCreate(string departmentId, string organizationId)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(UserCountMaintainService));

            try
            {
                await UpdateDepartmentUserCount(departmentId);
                await UpdateOrganizationLevelUserCount(departmentId, organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception in the service {ServiceName}. Exception Message: {ExceptionMessage}. Exception details: {StackTrace}.",
                    nameof(UserCountMaintainService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(UserCountMaintainService));
        }

        public async Task UpdateOrganizationLevelUserCount(string departmentId, string organizationId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(organizationId))
                {
                    var department = await GetDepartment(departmentId);
                    organizationId = department?.ParentOrganizationId;
                }

                if (!string.IsNullOrWhiteSpace(organizationId))
                {
                    var userCount = GetTotalDepartmentUserCount(organizationId);
                    var totalDepartmentUserLimit = GetTotalDepartmentUserLimit(organizationId);
                    var totalDepartmentStorageLimit = GetTotalDepartmentStorageLimit(organizationId);
                    var totalDepartmentLanguagesTokenLimit = GetTotalDepartmentLanguagesTokenLimit(organizationId);
                    var totalDepartmentManualTokenLimit = GetTotalDepartmentManualTokenLimit(organizationId);

                    var updateOrganizationLimitModel = new UpdateOrganizationLimitModel
                    {
                        OrganizationId = organizationId,
                        TotalDepartmentUserLimit = totalDepartmentUserLimit,
                        UserCount = userCount,
                        StorageLimit = totalDepartmentStorageLimit,
                        LanguageTokenLimit = totalDepartmentLanguagesTokenLimit,
                        ManualTokenLimit = totalDepartmentManualTokenLimit
                    };

                    await UpdateOrganizationUserCount(updateOrganizationLimitModel);
                    await UpdatePraxisClientSubscriptionUserCount(organizationId, userCount);
                    await ActionForUserLimitReached(organizationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in UpdateOrganizationLevelUserCount -> {ErrorMessage}.", ex.Message);
            }
        }

        public async Task ActionForUserLimitReached(string orgId)
        {
            if (!string.IsNullOrEmpty(orgId))
            {
                var orgData = _repository.GetItem<PraxisOrganization>(o => o.ItemId == orgId);
                if (orgData != null && orgData.TotalDepartmentUserLimit >= orgData.UserLimit)
                {
                    await SendEmailToUserLimitReached(orgData);
                }
            }
        }

        private async Task SendEmailToUserLimitReached(PraxisOrganization orgData)
        {
            try
            {
                if (orgData != null)
                {
                    var orgId = orgData.ItemId;
                    var securityContext = _securityContextProvider.GetSecurityContext();
                    var _translatedStringsAsDictionary = _uilmResourceKeyService
                        .GetResourceValueByKeyName(ReportConstants.PaymentInvoiceTranslationsKeys,
                            securityContext.Language);
                    var latestPraxisClientSubscriptionInfo =
                        _repository.GetItem<PraxisClientSubscription>(pcs => !pcs.IsMarkedToDelete && pcs.OrganizationId.Equals(orgId) && pcs.IsLatest && pcs.IsActive);
                    if (latestPraxisClientSubscriptionInfo != null)
                    {
                        var subscribePraxisUserList = _repository.GetItems<PraxisUser>(pu => pu.ItemId == orgData.AdminUserId || pu.ItemId == orgData.DeputyAdminUserId)?.ToList();

                        if (subscribePraxisUserList != null)
                        {
                            foreach (var subscribePraxisUser in subscribePraxisUserList)
                            {
                                var emailData = _emailDataBuilder.BuildUserUserLimitReachedEmailData(
                                    _translatedStringsAsDictionary[latestPraxisClientSubscriptionInfo.SubscriptionPackage],
                                    orgData.UserLimit.ToString(), orgId, subscribePraxisUser);

                                if (subscribePraxisUser != null)
                                {
                                    await _emailNotifierService.SendUserLimitReachedEmail(subscribePraxisUser, emailData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in SendEmailToUserLimitReached, {ExceptionMessage}", ex.Message);
            }
        }

        private async Task UpdateDepartmentUserCount(string departmentId)
        {
            var department = await GetDepartment(departmentId);
            if (department != null)
            {
                var newUserCount = GetDepartmentUserCount(departmentId);
                var inactiveUserCount = GetDepartmentUserCount(departmentId, false);

                if (department.MetaDataList == null) department.MetaDataList = new List<MetaDataKeyPairValue>();
                var inactiveUserCountInfo = department.MetaDataList.FirstOrDefault(m => m.Key == "InactiveUserCount");
                if (inactiveUserCountInfo != null)
                {
                    inactiveUserCountInfo.MetaData.Value = inactiveUserCount.ToString();
                }
                else
                {
                    department.MetaDataList.Add(new MetaDataKeyPairValue()
                    {
                        Key = "InactiveUserCount",
                        MetaData = new MetaValuePair()
                        {
                            Type = "int",
                            Value = inactiveUserCount.ToString()
                        }
                    });
                }

                var updates = new Dictionary<string, object>
                {
                    {"UserCount",  newUserCount},
                    {nameof(PraxisClient.MetaDataList), department.MetaDataList }
                };

                var builder = Builders<BsonDocument>.Filter;
                var updateFilters = builder.Eq("_id", departmentId);

                _ = await _changeLogService.UpdateChange(nameof(PraxisClient), updateFilters, updates);

                _logger.LogInformation("Updated user count in {EntityName}: {DepartmentId}.", nameof(PraxisClient), departmentId);
            }
        }

        private async Task<PraxisClient> GetDepartment(string departmentId)
        {
            return await _repository.GetItemAsync<PraxisClient>(pc => pc.ItemId == departmentId);
        }

        private async Task UpdateOrganizationUserCount(UpdateOrganizationLimitModel updateOrganizationLimitModel)
        {
            var updateData = new Dictionary<string, object>
                {
                    {"TotalDepartmentUserLimit", updateOrganizationLimitModel.TotalDepartmentUserLimit},
                    {"TotalDepartmentAuthorizedUserLimit", updateOrganizationLimitModel.TotalDepartmentUserLimit*2},
                    {"UserCount",  updateOrganizationLimitModel.UserCount},
                    {"TotalDepartmentAdditionalStorageLimit",  updateOrganizationLimitModel.StorageLimit},
                    {"TotalDepartmentAdditionalLanguageTokenLimit",  updateOrganizationLimitModel.LanguageTokenLimit},
                    {"TotalDepartmentAdditionalManualTokenLimit",  updateOrganizationLimitModel.ManualTokenLimit},
                };

            await _repository.UpdateAsync<PraxisOrganization>(po => po.ItemId == updateOrganizationLimitModel.OrganizationId, updateData);

            _logger.LogInformation("Updated user count in {EntityName}: {OrganizationId}.", nameof(PraxisOrganization), updateOrganizationLimitModel.OrganizationId);
        }

        private async Task UpdatePraxisClientSubscriptionUserCount(string organizationId, int userCount)
        {
            var updateData = new Dictionary<string, object>
                {
                    {"CreatedUserCount",  userCount}
                };

            await _repository.UpdateAsync<PraxisClientSubscription>(pcs => pcs.OrganizationId == organizationId && !pcs.IsMarkedToDelete, updateData);

            _logger.LogInformation("Updated user count in {EntityName} with organizationId: {OrganizationId}.", nameof(PraxisClientSubscription), organizationId);
        }

        private int GetTotalDepartmentUserCount(string organizationId)
        {
            var collection = ecapRepository.GetTenantDataContext().GetCollection<PraxisClient>($"PraxisClients");

            var userCount = collection.Aggregate()
                .Match(x => !x.IsMarkedToDelete && x.ParentOrganizationId == organizationId)
                .Group(
                        doc => doc.ParentOrganizationId,
                        group => new
                        {
                            OrganizationId = group.Key,
                            Total = group.Sum(y => y.UserCount)
                        }
                ).ToList().FirstOrDefault(c => c.OrganizationId == organizationId);

            return userCount != null ? userCount.Total : 0;
        }

        private int GetTotalDepartmentUserLimit(string organizationId)
        {
            var collection = ecapRepository.GetTenantDataContext().GetCollection<PraxisClient>($"PraxisClients");

            var totalDepartmentUserLimit = collection.Aggregate()
                .Match(x => !x.IsMarkedToDelete && x.ParentOrganizationId == organizationId)
                .Group(
                        doc => doc.ParentOrganizationId,
                        group => new
                        {
                            OrganizationId = group.Key,
                            Total = group.Sum(y => y.UserLimit)
                        }
                ).ToList().FirstOrDefault(c => c.OrganizationId == organizationId);

            return totalDepartmentUserLimit != null ? totalDepartmentUserLimit.Total : 0;
        }

        private double GetTotalDepartmentStorageLimit(string organizationId)
        {
            var collection = ecapRepository.GetTenantDataContext().GetCollection<PraxisClient>($"PraxisClients");

            var totalLimit = collection.Aggregate()
                .Match(x => !x.IsMarkedToDelete && x.ParentOrganizationId == organizationId)
                .Group(
                        doc => doc.ParentOrganizationId,
                        group => new
                        {
                            OrganizationId = group.Key,
                            Total = group.Sum(y => y.AdditionalStorage ?? 0)
                        }
                ).ToList().FirstOrDefault(c => c.OrganizationId == organizationId);

            return totalLimit != null ? totalLimit.Total : 0;
        }

        private double GetTotalDepartmentLanguagesTokenLimit(string organizationId)
        {
            var collection = ecapRepository.GetTenantDataContext().GetCollection<PraxisClient>($"PraxisClients");

            var totalLimit = collection.Aggregate()
                .Match(x => !x.IsMarkedToDelete && x.ParentOrganizationId == organizationId)
                .Group(
                        doc => doc.ParentOrganizationId,
                        group => new
                        {
                            OrganizationId = group.Key,
                            Total = group.Sum(y => y.AdditionalLanguagesToken ?? 0)
                        }
                ).ToList().FirstOrDefault(c => c.OrganizationId == organizationId);

            return totalLimit != null ? totalLimit.Total : 0;
        }

        private double GetTotalDepartmentManualTokenLimit(string organizationId)
        {
            var collection = ecapRepository.GetTenantDataContext().GetCollection<PraxisClient>($"PraxisClients");

            var totalLimit = collection.Aggregate()
                .Match(x => !x.IsMarkedToDelete && x.ParentOrganizationId == organizationId)
                .Group(
                        doc => doc.ParentOrganizationId,
                        group => new
                        {
                            OrganizationId = group.Key,
                            Total = group.Sum(y => y.AdditionalManualToken ?? 0)
                        }
                ).ToList().FirstOrDefault(c => c.OrganizationId == organizationId);

            return totalLimit != null ? totalLimit.Total : 0;
        }

        private int GetDepartmentUserCount(string clientId, bool active = true)
        {
            var userCount = _repository.GetItems<PraxisUser>
                (x => !x.IsMarkedToDelete && x.ClientList != null && x.ClientList.Any(c => c.ClientId == clientId && c.IsPrimaryDepartment) && x.Active == active)?.Count() ?? 0;

            return userCount;
        }
    }
}
