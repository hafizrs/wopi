using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsAdmins
{
    public class RiqsAdminsCreateUpdateService : IRiqsAdminsCreateUpdateService
    {
        private readonly ILogger<RiqsAdminsCreateUpdateService> _logger;
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IProcessUserDataByUam _processUserDataByUamService;
        private readonly IChangeLogService _changeLogService;
        private readonly INotificationService _notificationService;

        public RiqsAdminsCreateUpdateService(
            ILogger<RiqsAdminsCreateUpdateService> logger,
            IRepository repository,
            IMongoSecurityService mongoSecurityService,
            IProcessUserDataByUam processUserDataByUamService,
            IChangeLogService changeLogService,
            INotificationService notificationService
        )
        {
            _logger = logger;
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _processUserDataByUamService = processUserDataByUamService;
            _changeLogService = changeLogService;
            _notificationService = notificationService;
        }

        public async Task InitiateAdminBUpdateOnNewDepartmentAdd(string orgId)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(RiqsAdminsCreateUpdateService));
            try
            {
                var adminBUserIds = GetOrganizationAdminUserIds(orgId);
                if (adminBUserIds != null && adminBUserIds.Count > 0)
                {
                    var departments = GetAllDepartments(orgId);
                    var orgData = GetOrganization(orgId);
                    if (departments != null && departments.Count > 0)
                    {
                        var commonRoles = GetCommonRoles(orgId, departments);
                        foreach (var userId in adminBUserIds)
                        {
                            var praxisUser = await GetPraxisUser(userId);
                            if (praxisUser != null)
                            {
                                var primaryDepartment = GetPrimaryDepartment(praxisUser.ClientList);
                                var roles = commonRoles;
                                if (praxisUser.ItemId.Equals(orgData.AdminUserId) || praxisUser.ItemId.Equals(orgData.DeputyAdminUserId))
                                {
                                    roles = GetAllRoles(commonRoles, primaryDepartment);
                                }
                                roles = praxisUser.Roles.Union(roles).ToList();
                                await UpdateUserDataByUamService(userId, roles);
                                await UpdatePraxisUserRelatedData(userId, roles, orgId, departments, primaryDepartment);

                                var result = new
                                {
                                    NotifiySubscriptionId = userId,
                                    Success = true
                                };
                                await _notificationService.UserLogOutNotification(true, userId, result, "UserUpdate", "RolesUpdated");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception in the service {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(RiqsAdminsCreateUpdateService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(RiqsAdminsCreateUpdateService));
        }

        public async Task UpdateAdminBRolesOnOrganzationAdminDeputyAdminChange(List<string> adminBIds, string orgId)
        {
            _logger.LogInformation("Entered into the service {MethodName}.", nameof(UpdateAdminBRolesOnOrganzationAdminDeputyAdminChange));
            try
            {
                if (adminBIds != null && adminBIds.Count > 0)
                {
                    var departments = GetAllDepartments(orgId);
                    var orgData = GetOrganization(orgId);
                    if (departments != null && departments.Count > 0)
                    {
                        var commonRoles = GetCommonRoles(orgId, departments);
                        foreach (var praxisUserId in adminBIds)
                        {
                            var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.ItemId == praxisUserId && !pu.IsMarkedToDelete);
                            if (praxisUser != null)
                            {
                                var primaryDepartment = GetPrimaryDepartment(praxisUser.ClientList);
                                var roles = commonRoles;
                                if (praxisUser.ItemId.Equals(orgData.AdminUserId) || praxisUser.ItemId.Equals(orgData.DeputyAdminUserId))
                                {
                                    roles = GetAllRoles(commonRoles, primaryDepartment);
                                }
                                roles = praxisUser.Roles.Union(roles).ToList();
                                await UpdateUserDataByUamService(praxisUser.UserId, roles);
                                await UpdatePraxisUserRelatedData(praxisUser.UserId, roles, orgId, departments, primaryDepartment);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception in the service {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(RiqsAdminsCreateUpdateService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(RiqsAdminsCreateUpdateService));
        }

        public async Task CreateUpdateRiqsGroupAdmin(PraxisUser praxisuser, List<string> userCreatedOrgIds = null)
        {
            _logger.LogInformation("Entered into the service method {MethodName}.", nameof(CreateUpdateRiqsGroupAdmin));
            try
            {
                if (praxisuser == null) return;
                var groupAdmin = await GetRiqsGroupAdmin(praxisuser.UserId);
                var isGroupAdmin = praxisuser.Roles.Contains(RoleNames.GroupAdmin);
                var orgIds = praxisuser?.ClientList?.Select(c => c.ParentOrganizationId)?.Where(o => !string.IsNullOrEmpty(o))?.Distinct()?.ToList() ?? new List<string>();

                if (groupAdmin == null)
                {
                    if (!isGroupAdmin) return;

                    groupAdmin = new RiqsGroupAdmin()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        OrganizationIds = orgIds,
                        UserCreatedOrganizationIds = userCreatedOrgIds ?? new List<string>(),
                        IsGroupAdmin = isGroupAdmin,
                        PraxisUserId = praxisuser?.ItemId,
                        UserId = praxisuser?.UserId
                    };
                    await _repository.SaveAsync(groupAdmin);
                }
                else
                {
                    if (!isGroupAdmin)
                    {
                        var changableOrgIds = groupAdmin.OrganizationIds ?? new List<string>();
                        changableOrgIds.AddRange(groupAdmin.UserCreatedOrganizationIds ?? new List<string>());
                        changableOrgIds = changableOrgIds.Distinct().ToList();
                        await UpdatePraxisOrganizationData(changableOrgIds, groupAdmin.UserId);

                        await _repository.DeleteAsync<RiqsGroupAdmin>(r => r.ItemId == groupAdmin.ItemId);
                        return;
                    }
                    groupAdmin.OrganizationIds = orgIds;
                    groupAdmin.IsGroupAdmin = isGroupAdmin;

                    if (userCreatedOrgIds?.Count > 0)
                    {
                        userCreatedOrgIds.AddRange(groupAdmin.UserCreatedOrganizationIds ?? new List<string>());
                        userCreatedOrgIds = userCreatedOrgIds.Distinct().ToList();
                        groupAdmin.UserCreatedOrganizationIds = userCreatedOrgIds;
                    }
                    await _repository.UpdateAsync(o => o.ItemId == groupAdmin.ItemId, groupAdmin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception in the service {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(RiqsAdminsCreateUpdateService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service method {MethodName}.", nameof(CreateUpdateRiqsGroupAdmin));
        }

        public async Task InitiateGroupAdminUpdateOnNewOrganizationAdd(string orgId, string userId)
        {
            _logger.LogInformation("Entered into the service method {MethodName}.", nameof(InitiateGroupAdminUpdateOnNewOrganizationAdd));
            try
            {
                var pu = await GetPraxisUser(userId);
                if (pu?.Roles?.Contains(RoleNames.GroupAdmin) != true) return;

                await CreateUpdateRiqsGroupAdmin(pu, new List<string>() { orgId });

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in the service {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(RiqsAdminsCreateUpdateService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service method {MethodName}.", nameof(InitiateGroupAdminUpdateOnNewOrganizationAdd));
        }

        public async Task<List<PraxisUser>> InitiateGroupAdminUpdateOnNewDepartmentAdd(string deptId, string orgId)
        {
            _logger.LogInformation("Entered into the service method {MethodName}.", nameof(InitiateGroupAdminUpdateOnNewOrganizationAdd));
            try
            {
                var praxisUsers = new List<PraxisUser>();
                var org = await _repository.GetItemAsync<PraxisOrganization>(c => c.ItemId == orgId && !c.IsMarkedToDelete);
                var department = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == deptId && !c.IsMarkedToDelete);
                var groupAdmins = GetRiqsGroupAdminByOrgId(orgId);
                foreach (var groupAdmin in groupAdmins)
                {
                    var pu = await GetPraxisUser(groupAdmin.UserId);
                    if (pu.Roles.Contains(RoleNames.GroupAdmin))
                    {
                        var clientList = pu.ClientList.ToList();
                        var clientInfo = new PraxisClientInfo
                        {
                            ClientId = department?.ItemId,
                            ClientName = department?.ClientName,
                            IsPrimaryDepartment = false,
                            ParentOrganizationId = orgId,
                            ParentOrganizationName = org.ClientName,
                            IsCreateProcessGuideEnabled = department.Navigations.Any(nav => nav.Name == "PROCESS_GUIDE"),
                            Roles = new[] { RoleNames.PowerUser }
                        };
                        if (clientList?.Find(c => c.ClientId == deptId) == null)
                        {
                            clientList.Add(clientInfo);
                            pu.ClientList = clientList;

                            var roles = pu.Roles.ToList();
                            roles.AddRange(GetPowerUserNavRoles(new List<string>() { deptId }));
                            roles = roles.Distinct().ToList();
                            pu.Roles = roles;
                        }
                    }
                    //await _userUpdateService.ProcessData(pu, null);
                    praxisUsers.Add(pu);
                }
                return praxisUsers;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Exception in the service {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(RiqsAdminsCreateUpdateService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service method {MethodName}.", nameof(InitiateGroupAdminUpdateOnNewOrganizationAdd));

            return null;
        }

        private async Task<RiqsGroupAdmin> GetRiqsGroupAdmin(string userId)
        {
            return await _repository.GetItemAsync<RiqsGroupAdmin>(o => o.UserId == userId && !o.IsMarkedToDelete);
        }

        private List<RiqsGroupAdmin> GetRiqsGroupAdminByOrgId(string orgId)
        {
            return _repository.GetItems<RiqsGroupAdmin>
                    (o => !o.IsMarkedToDelete && ((o.OrganizationIds != null && o.OrganizationIds.Contains(orgId)
                    || (o.UserCreatedOrganizationIds != null && o.UserCreatedOrganizationIds.Contains(orgId))))).ToList();
        }

        private List<PraxisClient> GetAllDepartments(string orgId)
        {
            return _repository.GetItems<PraxisClient>(d => d.ParentOrganizationId == orgId && !d.IsMarkedToDelete).ToList();
        }

        private List<string> GetOrganizationAdminUserIds(string orgId)
        {
            var organization = _repository.GetItem<PraxisOrganization>(o => o.ItemId == orgId && !o.IsMarkedToDelete);
            return organization?.AdminBIds?.Select(o => o.UserId).ToList();
        }

        private async Task<PraxisUser> GetPraxisUser(string userId)
        {
            return await _repository.GetItemAsync<PraxisUser>(pu => pu.UserId == userId && !pu.IsMarkedToDelete);
        }

        private PraxisClientInfo GetPrimaryDepartment(IEnumerable<PraxisClientInfo> clientList)
        {
            return clientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
        }

        private List<String> GetAllRoles(List<string> commonRoles, PraxisClientInfo primaryDepartment)
        {
            var roles = new List<string>();
            roles.AddRange(commonRoles);

            if (primaryDepartment != null)
            {
                roles.Add($"{RoleNames.PoweruserPayment}_{primaryDepartment.ClientId}");
            }

            return roles;
        }

        private List<string> GetCommonRoles(string orgId, List<PraxisClient> departments)
        {
            var departmentIds = departments.Select(x => x.ItemId).ToList();

            var adminBRole = $"{RoleNames.AdminB_Dynamic}_{orgId}";
            var clientAdminAccessRoles = GetClientAdminAccessRoles(departmentIds);
            var clientAdminNavRoles = GetPowerUserNavRoles(departmentIds);
            var openOrgRoles = GetOpenOrgRoles(departments);
            List<string> roles = new List<string>
            {
                RoleNames.AppUser, RoleNames.Anonymous,
                RoleNames.AdminB, RoleNames.PowerUser,
                RoleNames.ClientSpecific,
                adminBRole
            };
            roles.AddRange(clientAdminAccessRoles);
            roles.AddRange(clientAdminNavRoles);
            roles.AddRange(openOrgRoles);

            return roles;
        }

        private List<String> GetClientAdminAccessRoles(List<String> departmentIds)
        {
            List<string> roles = new List<string>();

            departmentIds.ForEach((id) =>
            {
                var role = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, id);
                roles.Add(role);
            });

            return roles;
        }

        private List<String> GetClientReadAccessRoles(List<String> departmentIds)
        {
            List<string> roles = new List<string>();

            departmentIds.ForEach((id) =>
            {
                var role = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, id);
                roles.Add(role);
            }
            );

            return roles;
        }

        private List<String> GetClientManagerAccessRoles(List<String> departmentIds)
        {
            List<string> roles = new List<string>();

            departmentIds.ForEach((id) =>
            {
                var role = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, id);
                roles.Add(role);
            });

            return roles;
        }

        private List<String> GetPowerUserNavRoles(List<String> departmentIds)
        {
            List<string> roles = new List<string>();

            departmentIds.ForEach((id) =>
            {
                var role = $"{RoleNames.PowerUser_Nav}_{id}";
                roles.Add(role);
            });

            return roles;
        }

        private List<String> GetOpenOrgRoles(List<PraxisClient> departments)
        {
            List<string> roles = new List<string>();

            departments.ForEach((department) =>
            {
                if (!department.IsOpenOrganization.Value)
                {
                    var role = $"{RoleNames.Open_Organization}_{department.ItemId}";
                    roles.Add(role);
                }
            });

            return roles;
        }

        private async Task<bool> UpdateUserDataByUamService(string userId, List<string> roles)
        {
            var userData = PrepareUserDataForUamService(userId, roles);
            return await _processUserDataByUamService.UpdateData(userData);
        }

        private PersonInformation PrepareUserDataForUamService(string userId, List<string> roles)
        {
            var userData = new PersonInformation
            {
                UserId = userId,
                Roles = roles.ToArray()
            };

            return userData;
        }

        private async Task UpdatePraxisOrganizationData(List<string> orgIds, string userId)
        {
            if (orgIds == null || orgIds?.Count == 0) return;
            var orgDatas = _repository.GetItems<PraxisOrganization>(o => orgIds.Contains(o.ItemId)).ToList();
            foreach (var org in orgDatas)
            {
                var idsAllowedToRead = org.IdsAllowedToRead ?? new string[] { };
                if (!idsAllowedToRead.Contains(userId)) continue;
                idsAllowedToRead = idsAllowedToRead.Where(id => id != userId).ToArray();

                var builder = Builders<BsonDocument>.Filter;
                var updateFilters = builder.Eq("_id", org.ItemId);
                var updates = new Dictionary<string, object>
                {
                    { nameof(PraxisOrganization.IdsAllowedToRead), idsAllowedToRead },
                };

                await _changeLogService.UpdateChange(EntityName.PraxisOrganization, updateFilters, updates);
            }
        }

        private async Task UpdatePraxisUserRelatedData(
            string userId,
            List<string> roles,
            string orgId,
            List<PraxisClient> departments,
            PraxisClientInfo primaryDepartment)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.Eq("UserId", userId);

            var praxisUser = PreparePraxisUserUpdateData(userId, roles, orgId, departments, primaryDepartment);
            var updateData = new Dictionary<string, object>
                    {
                        {"Roles",  praxisUser.Roles},
                        {"ClientList",  praxisUser.ClientList},
                        {"RolesAllowedToRead",  praxisUser.RolesAllowedToRead},
                        {"RolesAllowedToUpdate",  praxisUser.RolesAllowedToUpdate}
                    };

            await _changeLogService.UpdateChange(EntityName.PraxisUser, updateFilters, updateData);
            await UpdatePraxisUserDtoData(userId, praxisUser);
        }

        private PraxisUser PreparePraxisUserUpdateData(
            string userId,
            List<string> roles,
            string orgId,
            List<PraxisClient> departments,
            PraxisClientInfo primaryDepartment)
        {
            var departmentIds = departments.Select(x => x.ItemId).ToList();

            var clientAdminAccessRoles = GetClientAdminAccessRoles(departmentIds);
            var clientReadAccessRoles = GetClientReadAccessRoles(departmentIds);
            var clientManagerAccessRoles = GetClientManagerAccessRoles(departmentIds);

            List<string> rolesAllowToRead = new List<string> { RoleNames.Admin, RoleNames.TaskController };
            rolesAllowToRead.AddRange(clientAdminAccessRoles);
            rolesAllowToRead.AddRange(clientManagerAccessRoles);
            rolesAllowToRead.AddRange(clientReadAccessRoles);

            List<string> rolesAllowToUpdate = new List<string> { RoleNames.Admin, RoleNames.TaskController };
            rolesAllowToUpdate.AddRange(clientAdminAccessRoles);
            rolesAllowToUpdate.AddRange(clientManagerAccessRoles);

            var clientList = GetPraxisUserClientList(orgId, departments, primaryDepartment);

            var userData = new PraxisUser
            {
                UserId = userId,
                Roles = roles,
                ClientList = clientList,
                RolesAllowedToRead = rolesAllowToRead.ToArray(),
                RolesAllowedToUpdate = rolesAllowToUpdate.ToArray()
            };

            return userData;
        }

        private List<PraxisClientInfo> GetPraxisUserClientList(
            string orgId,
            List<PraxisClient> departments,
            PraxisClientInfo primaryDepartment)
        {
            var organization = GetOrganization(orgId);
            var organizationName = organization != null ? organization.ClientName : "";

            List<PraxisClientInfo> clientList = new List<PraxisClientInfo>();
            departments.ForEach((department) =>
            {
                var clientInfo = new PraxisClientInfo
                {
                    ClientId = department.ItemId,
                    ClientName = department.ClientName,
                    IsPrimaryDepartment = department.ItemId == primaryDepartment?.ClientId,
                    ParentOrganizationId = orgId,
                    ParentOrganizationName = organizationName,
                    IsCreateProcessGuideEnabled = department.Navigations.Any(nav => nav.Name == "PROCESS_GUIDE"),
                    Roles = new[] { RoleNames.PowerUser }
                };
                clientList.Add(clientInfo);
            });

            return clientList;
        }

        private PraxisOrganization GetOrganization(string id)
        {
            return _repository.GetItem<PraxisOrganization>(o => o.ItemId == id && !o.IsMarkedToDelete);
        }

        private async Task UpdatePraxisUserDtoData(string userId, PraxisUser praxisUser)
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.Eq("UserId", userId);
            var updateData = new Dictionary<string, object>
                {
                    {"Clients", praxisUser.ClientList.Select(x => new PraxisClientInfoDto { ClientId = x.ClientId, ClientName = x.ClientName }).ToList() },
                    {"RolesAllowedToRead",  praxisUser.RolesAllowedToRead},
                    {"RolesAllowedToUpdate",  praxisUser.RolesAllowedToUpdate}
                };

            await _changeLogService.UpdateChange(EntityName.PraxisUserDto, updateFilters, updateData);
        }
    }
}
