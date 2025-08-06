using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisOrganizationCreateUpdateEventService : IPraxisOrganizationCreateUpdateEventService
    {
        private readonly ILogger<PraxisOrganizationCreateUpdateEventService> _logger;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;
        private readonly IOrganizationDataProcessService _organizationDataProcessService;
        private readonly IPrepareNewRole _prepareNewRoleService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;
        private readonly ILincensingService _lincensingService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly ISecurityContextProvider _securityContextProvider;

        public PraxisOrganizationCreateUpdateEventService(
            ILogger<PraxisOrganizationCreateUpdateEventService> logger,
            IRepository repository,
            IChangeLogService changeLogService,
            IOrganizationDataProcessService organizationDataProcessService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService,
            IPrepareNewRole prepareNewRoleService,
            ILincensingService lincensingService,
            IOrganizationSubscriptionService organizationSubscriptionService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            ISecurityContextProvider securityContextProvider
        )
        {
            _logger = logger;
            _repository = repository;
            _changeLogService = changeLogService;
            _organizationDataProcessService = organizationDataProcessService;
            _riqsAdminsCreateUpdateService = riqsAdminsCreateUpdateService;
            _prepareNewRoleService = prepareNewRoleService;
            _lincensingService = lincensingService;
            _organizationSubscriptionService = organizationSubscriptionService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> InitiateOrganizationCreateUpdateAfterEffects(PraxisOrganization previousOrgData, string eventType)
        {
            try
            {
                var response = true;
                if (eventType.Equals(PraxisEventType.OrganizationCreatedEvent))
                {
                    response = await _organizationDataProcessService.InitiateOrganizationLogoUploadPostProcess(previousOrgData);
                    await ProcessCreatedOrganizationEffects(previousOrgData);
                }
                if (eventType.Equals(PraxisEventType.OrganizationUpdatedEvent))
                {
                    var orgData = GetOrganization(previousOrgData.ItemId);
                    if (orgData != null)
                    {
                        if (JsonConvert.SerializeObject(orgData.Logo) != JsonConvert.SerializeObject(previousOrgData.Logo))
                        {
                            response = await _organizationDataProcessService.InitiateOrganizationLogoUploadPostProcess(orgData);
                        }
                        await ProcessUpdatedOrganizationEffects(orgData, previousOrgData);
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred on InitiateOrganizationCreateUpdateAfterEffects --> Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task ProcessCreatedOrganizationEffects(PraxisOrganization organizationData)
        {
            CreateOrgWideRoles(organizationData.ItemId);
            var adminBRole = $"{RoleNames.AdminB_Dynamic}_{organizationData.ItemId}";
            await UpdateOrgDataPermissions(organizationData.ItemId, adminBRole);
            await _organizationDataProcessService.ProcessOrganizationStorageSpaceAllocation(organizationData);
            await SaveSubscriptionDependencies(organizationData);
            await UpdateGroupAdminUser(organizationData.ItemId);
        }

        private async Task UpdateGroupAdminUser(string orgId)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            await _riqsAdminsCreateUpdateService.InitiateGroupAdminUpdateOnNewOrganizationAdd(orgId, userId);
        }

        private async Task SaveSubscriptionDependencies(PraxisOrganization organizationData)
        {
            var totalStorage = organizationData.UserLimit * 0.5;
            await _lincensingService.ProcessStorageLicensing(organizationData.ItemId, totalStorage);

            var subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(organizationData.ItemId);

            var totalToken = (subs?.TokenSubscription?.IncludedTokenInMillion ?? 0) + (subs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
            var totalManualToken = (subs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0) + (subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);

            var organizationSubsPayload = new OrganizationSubscription
            {
                OrganizationId = organizationData.ItemId,
                TotalTokenSize = totalToken,
                TotalStorageSize = totalStorage,
                TokenOfOrganization = totalToken,
                StorageOfOrganization = totalStorage,
                TokenOfUnits = 0,
                StorageOfUnits = 0,
                SubscriptionDate = subs?.SubscriptionDate ?? DateTime.UtcNow.Date,
                SubscriptionExpirationDate = subs?.SubscriptionExpirationDate ?? DateTime.UtcNow.Date.AddMonths(12).AddDays(-1),
                IsTokenApplied = subs?.IsTokenApplied ?? false,
                TotalManualTokenSize = totalManualToken,
                ManualTokenOfOrganization = totalManualToken,
                ManualTokenOfUnits = 0,
                IsManualTokenApplied = subs?.IsManualTokenApplied ?? false
            };
            await _organizationSubscriptionService.SaveOrganizationSubscription(organizationSubsPayload);
        }

        private async Task ProcessUpdatedOrganizationEffects(PraxisOrganization organizationData, PraxisOrganization previousOrgData)
        {
            var departments = GetDepartments(organizationData.ItemId);
            if (organizationData.Address != null)
            {
                await UpdateDepartmentAddress(departments, organizationData.Address);
            }

            await UpdateDepartmentOrganizationName(departments, organizationData.ClientName);
            await UpdatePraxisUserOrganizationName(organizationData.ItemId, organizationData.ClientName);
            await UpdateClientSubscriptionOrganizationProperties(organizationData);
            // await UpdateUserRolesForAdminAndDeputyUser(organizationData, previousOrgData);
        }

        private void CreateOrgWideRoles(string orgId)
        {
            var roles = new List<(string, string)>
            {
                ($"{RoleNames.AdminB_Dynamic}_{orgId}", $"{RoleNames.AdminB}"),
                ($"{RoleNames.Organization_Read_Dynamic}_{orgId}", $"{RoleNames.Organization_Read_Dynamic}")
            };

            roles.ForEach(role => _prepareNewRoleService.SaveRole(
                role.Item1,
                orgId,
                role.Item2,
                true)
            );
        }

        private async Task UpdateOrgDataPermissions(string orgId, string orgDynamicRole)
        {
            var orgData = GetOrganization(orgId);

            var rolesAllowedToRead = new List<string>();
            rolesAllowedToRead.AddRange(orgData.RolesAllowedToRead);
            rolesAllowedToRead.Add(orgDynamicRole);
            orgData.RolesAllowedToRead = rolesAllowedToRead.Distinct().ToArray();

            await _repository.UpdateAsync(o => o.ItemId.Equals(orgId), orgData);
        }

        private async Task UpdateDepartmentAddress(List<PraxisClient> departments, PraxisAddress orgAddress)
        {
            var targetedDepartmentIds =
                departments.Where(d => d.IsSameAddressAsParentOrganization).Select(d => d.ItemId).ToList();
            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.In("_id", targetedDepartmentIds);

            var updates = new Dictionary<string, object>
            {
                {"Address",  orgAddress}
            };

            await _changeLogService.UpdateChange(EntityName.PraxisClient, updateFilters, updates);
        }

        private async Task UpdateDepartmentOrganizationName(List<PraxisClient> departments, string orgName)
        {
            var targetedDepartmentIds = departments.Select(d => d.ItemId).ToList();

            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.In("_id", targetedDepartmentIds);

            var updates = new Dictionary<string, object>
            {
                {"ParentOrganizationName",  orgName}
            };

            await _changeLogService.UpdateChange(EntityName.PraxisClient, updateFilters, updates);
        }

        private async Task UpdatePraxisUserOrganizationName(string orgId, string orgName)
        {
            var targetedPraxisUsers = _repository.GetItems<PraxisUser>
                        (x => !x.IsMarkedToDelete && x.ClientList != null
                        && x.ClientList.Any(c => c.ParentOrganizationId == orgId)).ToList();

            foreach (var praxisUser in targetedPraxisUsers)
            {
                foreach (var client in praxisUser.ClientList)
                {
                    if (client.ParentOrganizationId == orgId)
                    {
                        client.ParentOrganizationName = orgName;
                    }
                }
                var filterBuilder = Builders<BsonDocument>.Filter;
                var updateFilters = filterBuilder.Eq("_id", praxisUser.ItemId);

                var updates = new Dictionary<string, object>
                {
                    {"ClientList",  praxisUser.ClientList}
                };

                await _changeLogService.UpdateChange(EntityName.PraxisUser, updateFilters, updates);
            }
        }

        private async Task UpdateClientSubscriptionOrganizationProperties(PraxisOrganization orgData)
        {
            if (orgData != null)
            {
                var necessaryIds = _repository.GetItems<PraxisUser>
                    (pu => pu.ItemId == orgData.AdminUserId || pu.ItemId == orgData.DeputyAdminUserId)?
                    .Select(pu => pu.UserId)?.ToArray();
                var necessaryRoles = new List<string>() { RoleNames.Admin, RoleNames.AdminB }.ToArray();

                var updateData = new Dictionary<string, object>
                {
                    { "OrganizationName",  orgData.ClientName },
                    { "OrganizationEmail",  orgData.ContactEmail },
                    { "IdsAllowedToRead", necessaryIds },
                    { "RolesAllowedToRead", necessaryRoles }
                };

                await _repository.UpdateManyAsync<PraxisClientSubscription>(c => !c.IsMarkedToDelete && c.OrganizationId == orgData.ItemId, updateData);
            }
        }

        private async Task UpdateUserRolesForAdminAndDeputyUser(PraxisOrganization orgData, PraxisOrganization previousOrgData)
        {
            var adminBIds = new List<string>();
            if (orgData.AdminUserId != previousOrgData.AdminUserId)
            {
                if (!string.IsNullOrEmpty(orgData.AdminUserId)) adminBIds.Add(orgData.AdminUserId);
                if (!string.IsNullOrEmpty(previousOrgData.AdminUserId)) adminBIds.Add(previousOrgData.AdminUserId);
            }
            if (orgData.DeputyAdminUserId != previousOrgData.DeputyAdminUserId)
            {
                if (!string.IsNullOrEmpty(orgData.DeputyAdminUserId)) adminBIds.Add(orgData.DeputyAdminUserId);
                if (!string.IsNullOrEmpty(previousOrgData.DeputyAdminUserId)) adminBIds.Add(previousOrgData.DeputyAdminUserId);
            }
            adminBIds = adminBIds.Distinct().ToList();
            await _riqsAdminsCreateUpdateService.UpdateAdminBRolesOnOrganzationAdminDeputyAdminChange(adminBIds, orgData.ItemId);
        }

        private List<PraxisClient> GetDepartments(string orgId)
        {
            return _repository.GetItems<PraxisClient>(p => p.ParentOrganizationId == orgId && !p.IsMarkedToDelete).ToList();
        }

        private PraxisOrganization GetOrganization(string orgId)
        {
            return _repository.GetItem<PraxisOrganization>(p => p.ItemId == orgId);
        }
    }
}
