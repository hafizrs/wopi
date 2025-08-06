using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisUserService : IPraxisUserService, IDeleteDataForClientInCollections
    {
        private readonly ILogger<PraxisUserService> _logger;
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IUserPersonService _userPersonService;

        public PraxisUserService(
            ILogger<PraxisUserService> logger,
            IRepository repository,
            IMongoSecurityService mongoSecurityService,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            ICommonUtilService commonUtilService,
            IUserPersonService userPersonService
        )
        {
            _logger = logger;
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
            _commonUtilService = commonUtilService;
            _userPersonService = userPersonService;
        }

        public async Task<PraxisUser> GetPraxisUser(string userId)
        {
            return await Task.Run(() =>
            {
                return _repository.GetItem<PraxisUser>(
                    pu => pu.UserId.Equals(userId)
                );
            });
        }

        public List<PraxisUser> GetControlledUsers(string praxisClientId)
        {
            var praxisUsers = _repository.GetItems<PraxisUser>(
                pu => pu.ClientId.Equals(praxisClientId) &&
                      (pu.Roles.Contains(RoleNames.MpaGroup1) || pu.Roles.Contains(RoleNames.MpaGroup2))
            ).ToList();

            return praxisUsers;
        }

        public List<PraxisUser> GetControllingUsers(string praxisClientId)
        {
            var praxisUsers = _repository.GetItems<PraxisUser>(
                pu => pu.ClientId.Equals(praxisClientId) && pu.Roles.Contains(RoleNames.Leitung)
            ).ToList();

            return praxisUsers;
        }

        public async Task<List<PraxisUser>> GetAllPraxisUsers()
        {
            return await Task.Run(() =>
            {
                var praxisUsers = _repository.GetItems<PraxisUser>().ToList();

                return praxisUsers;
            });
        }

        public async Task<Person> GetPersonByUserId(string userId)
        {
            return await Task.Run(() =>
            {
                var person = _repository.GetItems<Person>(
                    p => p.CreatedBy.Equals(userId)
                ).FirstOrDefault();

                return person;
            });
        }

        public async Task UpdatePraxisUserImage(string userId, PraxisImage image)
        {
            PraxisUser user = _repository.GetItem<PraxisUser>(
                pu => pu.UserId.Equals(userId)
            );

            user.Image = image;

            var updates = new Dictionary<string, object>
            {
                {"Image", image},
                {"Tags", new string[] {TagName.IsValidPraxisUser}},
            };

            await _repository.UpdateAsync<PraxisUser>(pu => pu.ItemId == userId, updates);
        }

        public bool UpdatePraxisUserRoles(string itemId)
        {
            PraxisUser praxisUser = _repository.GetItem<PraxisUser>(
                pu => pu.ItemId.Equals(itemId) && !pu.IsMarkedToDelete
            );

            if (!string.IsNullOrEmpty(praxisUser?.UserId))
            {
                User user = _repository.GetItem<User>(
                    pu => pu.ItemId.Equals(praxisUser.UserId) && !pu.IsMarkedToDelete
                );

                praxisUser.Roles = user.Roles;

                _repository.Update<PraxisUser>(pu => pu.ItemId.Equals(itemId), praxisUser);
                _logger.LogInformation("Roles property has been successfully updated for PraxisUser entity with itemId: {ItemId} and roles: {Roles}.", itemId, JsonConvert.SerializeObject(praxisUser.Roles));
                return false;
            }

            return false;
        }

        public bool UpdatePraxisUserLatestClientProperty(PraxisUser praxisUserInfo)
        {
            try
            {
                if (!string.IsNullOrEmpty(praxisUserInfo?.ItemId))
                {
                    foreach (var client in praxisUserInfo.ClientList)
                    {
                        if (client.IsLatest)
                            client.IsLatest = false;
                    }

                    _repository.Update<PraxisUser>(x => x.ItemId.Equals(praxisUserInfo.ItemId), praxisUserInfo);
                    _logger.LogInformation("User latest client data updated successfully for user {UserId} and updated clientList {ClientList}", praxisUserInfo.UserId, praxisUserInfo.ClientList);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdatePraxisUserLatestClientProperty failed -> for {Message}.", ex.Message);
                return false;
            }
        }

        public void RoleAssignToPraxisUser(string praxisUserId, IEnumerable<PraxisClientInfo> clientList,
            bool isTechnicalClient = false)
        {
            var roles = new List<string>
            {
                isTechnicalClient ? RoleNames.TechnicalClient : RoleNames.ClientSpecific, RoleNames.AppUser,
                RoleNames.Anonymous
            };

            PraxisUser praxisUser =
                _repository.GetItem<PraxisUser>(u => u.ItemId.Equals(praxisUserId) && !u.IsMarkedToDelete);

            if (praxisUser != null)
            {
                foreach (var client in clientList)
                {
                    var roleList = new List<string>();
                    var adminRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, client.ClientId);
                    var readRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, client.ClientId);
                    var managerRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, client.ClientId);
                    var eeGroup1RoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup1, client.ClientId);
                    var eeGroup2RoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup2, client.ClientId);

                    roleList.Add(eeGroup1RoleName);
                    roleList.Add(eeGroup2RoleName);
                    PrepareRole(roleList, client.ClientId);

                    int isPowerUser = Array.IndexOf(client.Roles.ToArray(), RoleNames.PowerUser);
                    int isLeitung = Array.IndexOf(client.Roles.ToArray(), RoleNames.Leitung);
                    int isMpaGroup1 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup1);
                    int isMpaGroup2 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup2);


                    if (isPowerUser > -1)
                    {
                        roles.Add(adminRoleName);
                    }

                    if (isLeitung > -1)
                    {
                        roles.Add(managerRoleName);
                    }

                    if (isMpaGroup1 > -1 || isMpaGroup2 > -1)
                    {
                        roles.Add(readRoleName);
                    }

                    if (isMpaGroup1 > -1)
                    {
                        roles.Add(eeGroup1RoleName);
                    }

                    if (isMpaGroup2 > -1)
                    {
                        roles.Add(eeGroup2RoleName);
                    }
                }

                var combinedRoles = praxisUser.Roles.Concat(roles);
                praxisUser.Roles = combinedRoles.Distinct();

                UpdatedIdsAllowance(ref praxisUser);
                _repository.Update<PraxisUser>(p => p.ItemId.Equals(praxisUserId), praxisUser);

                var existingPerson =
                    _repository.GetItem<Person>(p => p.ItemId == praxisUserId && !p.IsMarkedToDelete);
                if (existingPerson != null)
                {
                    existingPerson.Roles = praxisUser.Roles.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"Roles", existingPerson.Roles}
                    };
                    _repository.UpdateAsync<Person>(p => p.ItemId == existingPerson.ItemId, updates).Wait();
                }

                var user = _repository.GetItem<User>(
                    u => u.Email == existingPerson.Email && !u.IsMarkedToDelete);
                if (user != null)
                {
                    CheckUserRoleMap(user, praxisUser.Roles.ToList());

                    user.Roles = praxisUser.Roles.ToArray();
                    var updates = new Dictionary<string, object>
                    {
                        {"Roles", user.Roles}
                    };

                    _repository.UpdateAsync<User>(u => u.ItemId == user.ItemId, updates).Wait();
                }
            }
            else
            {
                _logger.LogInformation("RoleAssignToPraxisUser Get NULL for praxisUserId {PraxisUserId}", praxisUserId);
            }
        }

        private void PrepareRole(List<string> roles, string clientId)
        {
            _logger.LogInformation("Going to prepare dynamic role with role: {Roles}.", string.Join(',', roles));
            foreach (var role in roles)
            {
                try
                {
                    var isRoleExists = _mongoSecurityService.IsRoleExist(role);
                    var openOrgRole = !isRoleExists ? PrepareRoleToRolesTable(role, clientId) : role;
                    var isExist = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == openOrgRole).Result;
                    if (!isExist)
                    {
                        var parents =
                            _roleHierarchyForPersonaRoleService.GetParentList(RoleNames.PowerUser);
                        var newRoleHierarchy = new RoleHierarchy
                        {
                            ItemId = Guid.NewGuid().ToString(),
                            Parents = parents.ToList(),
                            Role = openOrgRole
                        };
                        _repository.Save(newRoleHierarchy);
                        _logger.LogInformation("Data has been successfully inserted to {RoleHierarchy} entity with ItemId: {ItemId}.", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occurred during prepare dynamic role with role: {Role}. Exception Message: {Message}. Exception Details: {StackTrace}.", role, ex.Message, ex.StackTrace);
                }
            }
        }

        private string PrepareRoleToRolesTable(string roleName, string organizationId)
        {
            _logger.LogInformation("Going to save role: {RoleName} in {Entity} entity for Client: {OrganizationId}.", roleName, nameof(Role), organizationId);

            var securityContext = _securityContextProvider.GetSecurityContext();
            var rolesAllowTo = new string[] { "appuser" };
            var roleExist = _repository.ExistsAsync<Role>(r => r.RoleName == roleName).Result;
            if (!roleExist)
            {
                var newRole = new Role
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow,
                    CreatedBy = securityContext.UserId,
                    Language = "en-US",
                    LastUpdateDate = DateTime.UtcNow,
                    LastUpdatedBy = securityContext.UserId,
                    Tags = new[] { "built-in" },
                    TenantId = securityContext.TenantId,
                    RolesAllowedToRead = rolesAllowTo,
                    RolesAllowedToUpdate = rolesAllowTo,
                    RoleName = roleName,
                    IsDynamic = true
                };

                _repository.Save(newRole);
                _logger.LogInformation("Data has been successfully inserted to {Role} entity with role name: {RoleName} with ItemId: {ItemId}.", nameof(Role), roleName, newRole.ItemId);
                return roleName;
            }

            return roleName;
        }


        private void UpdatedIdsAllowance(ref PraxisUser praxisUser)
        {
            var IdsAllowedToRead = new List<string>();
            var IdsAllowedToUpdate = new List<string>();

            IdsAllowedToRead.AddRange(praxisUser.IdsAllowedToRead);
            IdsAllowedToRead.Add(praxisUser.UserId);

            IdsAllowedToUpdate.AddRange(praxisUser.IdsAllowedToUpdate);
            IdsAllowedToUpdate.Add(praxisUser.UserId);

            praxisUser.IdsAllowedToRead = IdsAllowedToRead.ToArray();
            praxisUser.IdsAllowedToUpdate = IdsAllowedToUpdate.ToArray();
        }

        public void AddRowLevelSecurity(string itemId, string[] clientIds)
        {
            var rolesAllowedToRead = new List<string>() { RoleNames.Admin, RoleNames.TaskController };
            var rolesAllowedToUpdate = new List<string>() { RoleNames.Admin, RoleNames.TaskController };

            foreach (var clientId in clientIds)
            {
                var clientAdminAccessRole =
                    _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
                var clientReadAccessRole =
                    _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
                var clientManagerAccessRole =
                    _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

                rolesAllowedToRead.Add(clientAdminAccessRole);
                rolesAllowedToRead.Add(clientReadAccessRole);
                rolesAllowedToRead.Add(clientManagerAccessRole);

                rolesAllowedToUpdate.Add(clientAdminAccessRole);
                rolesAllowedToUpdate.Add(clientManagerAccessRole);
            }

            var updates = new Dictionary<string, object>
            {
                {"RolesAllowedToRead", rolesAllowedToRead},
                {"RolesAllowedToUpdate", rolesAllowedToUpdate}
            };

            _repository.UpdateAsync<PraxisUser>(p => p.ItemId == itemId, updates).Wait();

            var existingPerson = _repository.GetItem<Person>(p => p.ItemId == itemId && !p.IsMarkedToDelete);
            if (existingPerson != null)
            {
                _repository.UpdateAsync<Person>(p => p.ItemId == existingPerson.ItemId, updates).Wait();
            }

            var existingUser =
                _repository.GetItem<User>(p => p.ItemId == existingPerson.CreatedBy && !p.IsMarkedToDelete);
            if (existingUser != null)
            {
                _repository.UpdateAsync<User>(p => p.ItemId == existingPerson.CreatedBy, updates).Wait();
            }
        }

        public void RoleReassignToPraxis(string praxisUserId, IEnumerable<PraxisClientInfo> clientList,
            bool isTechnicalClient = false)
        {
            var unassignRoles = new List<string>();
            var roles = new List<string>
            {
                isTechnicalClient ? RoleNames.TechnicalClient : RoleNames.ClientSpecific
            };

            PraxisUser praxisUser =
                _repository.GetItem<PraxisUser>(u => u.ItemId.Equals(praxisUserId) && !u.IsMarkedToDelete);

            if (praxisUser != null)
            {
                foreach (var client in clientList)
                {
                    var adminRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, client.ClientId);
                    var readRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, client.ClientId);
                    var managerRoleName =
                        _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, client.ClientId);

                    int isPowerUser = Array.IndexOf(client.Roles.ToArray(), RoleNames.PowerUser);
                    int isLeitung = Array.IndexOf(client.Roles.ToArray(), RoleNames.Leitung);
                    int isMpaGroup1 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup1);
                    int isMpaGroup2 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup2);

                    if (isPowerUser > -1)
                    {
                        roles.Add(adminRoleName);
                    }
                    else
                    {
                        unassignRoles.Add(adminRoleName);
                    }

                    if (isLeitung > -1)
                    {
                        roles.Add(managerRoleName);
                    }
                    else
                    {
                        unassignRoles.Add(managerRoleName);
                    }

                    if (isMpaGroup1 > -1 || isMpaGroup2 > -1)
                    {
                        roles.Add(readRoleName);
                    }
                    else
                    {
                        unassignRoles.Add(readRoleName);
                    }
                }

                _mongoSecurityService.UnassignRoleFromUser(Guid.Parse(praxisUser.UserId), unassignRoles, false);
                _logger.LogInformation("Roles property has been successfully updated to {User} entity with unassign roles: {UnassignRoles} and ItemId: {ItemId}.", nameof(User), JsonConvert.SerializeObject(unassignRoles), praxisUser.UserId);

                _mongoSecurityService.AssignRoleToUser(Guid.Parse(praxisUser.UserId), roles, false);
                _logger.LogInformation("Roles property has been successfully updated to {User} entity with assign roles: {Roles} and ItemId: {ItemId}.", nameof(User), JsonConvert.SerializeObject(roles), praxisUser.UserId);

                var user = _repository.GetItem<User>(u => u.ItemId == praxisUser.UserId && !u.IsMarkedToDelete);
                if (user != null)
                {
                    var existingPerson = _repository.GetItem<Person>(p =>
                        p.ItemId == praxisUser.ItemId && !p.IsMarkedToDelete);
                    if (existingPerson != null)
                    {
                        existingPerson.Roles = user.Roles.ToArray();
                        _repository.UpdateAsync<Person>(p => p.ItemId == existingPerson.ItemId, existingPerson).Wait();
                        _logger.LogInformation("Roles property has been successfully updated to {Entity} entity with roles: {Roles} and ItemId: {ItemId}.", nameof(Person), JsonConvert.SerializeObject(user.Roles), existingPerson.ItemId);
                    }

                    praxisUser.Roles = user.Roles.ToArray();
                    _repository.UpdateAsync<PraxisUser>(pu => pu.ItemId == praxisUser.ItemId, praxisUser).Wait();
                    _logger.LogInformation("Roles property has been successfully updated to {PraxisUser} entity with roles: {Roles} and ItemId: {ItemId}.", nameof(PraxisUser), JsonConvert.SerializeObject(user.Roles), praxisUser.ItemId);
                    CheckUserRoleMapForUpdateUser(user);
                }
            }
            else
            {
                _logger.LogInformation("RoleAssignToPraxisUser Get NULL for praxisUserId {PraxisUserId}", praxisUserId);
            }
        }

        public PraxisUser GetPraxisUserByUserId(string userId)
        {
            PraxisUser praxisUser = _repository.GetItem<PraxisUser>(
                pu => pu.UserId.Equals(userId) && !pu.IsMarkedToDelete
            );

            return praxisUser;
        }

        public PraxisUserDto GetPraxisUserDtoByUserId(string userId)
        {
            PraxisUserDto praxisUserDto = _repository.GetItem<PraxisUserDto>(
                pud => pud.UserId.Equals(userId) && !pud.IsMarkedToDelete
            );

            return praxisUserDto;
        }

        public bool UpdateUserActivationStatus(string userId, bool active = true, bool isEmailVerified = false)
        {
            PraxisUser praxisUser = GetPraxisUserByUserId(userId);

            if (praxisUser != null)
            {
                praxisUser.Active = active;
                if (isEmailVerified)
                {
                    praxisUser.IsEmailVerified = isEmailVerified;
                }

                _repository.Update<PraxisUser>(pu => pu.ItemId.Equals(praxisUser.ItemId), praxisUser);
                PraxisUserDto praxisUserDto = GetPraxisUserDtoByUserId(userId);
                if (praxisUserDto != null)
                {
                    praxisUserDto.IsActive = active;
                    _repository.Update<PraxisUserDto>(pud => pud.ItemId.Equals(praxisUserDto.ItemId), praxisUserDto);
                }

                return true;
            }

            return false;
        }

        public List<PraxisUser> GetControlledAndControllingUsers(string praxisClientId)
        {
            var praxisUsers = _repository.GetItems<PraxisUser>(
                pu => pu.ClientId.Equals(praxisClientId) &&
                      (pu.Roles.Contains(RoleNames.Leitung) ||
                       pu.Roles.Contains(RoleNames.MpaGroup1) && !pu.IsMarkedToDelete)
            ).ToList();

            return praxisUsers;
        }

        private void CheckUserRoleMap(User user, List<string> roles)
        {
            var extraRoles = roles.Except(user.Roles);
            foreach (var extraRole in extraRoles)
            {
                var isExist = _repository
                    .ExistsAsync<UserRoleMap>(u => u.UserId == user.ItemId && u.RoleName == extraRole).Result;
                if (!isExist)
                {
                    var newUserRoleMap = new UserRoleMap
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        RoleName = extraRole,
                        UserName = user.UserName,
                        UserId = user.ItemId
                    };

                    _repository.Save(newUserRoleMap);
                }
            }
        }

        private void CheckUserRoleMapForUpdateUser(User user)
        {
            var userRoleMap =
                _repository.GetItems<UserRoleMap>(u => u.UserId == user.ItemId && !u.IsMarkedToDelete);

            var roleMapRoles = userRoleMap.Select(u => u.RoleName).ToList();
            var userRoles = user.Roles.ToList();
            var extraRoles = userRoles.Except(roleMapRoles);

            foreach (var extraRole in extraRoles)
            {
                var isExist = _repository
                    .ExistsAsync<UserRoleMap>(u => u.UserId == user.ItemId && u.RoleName == extraRole).Result;
                if (isExist) continue;
                var newUserRoleMap = new UserRoleMap
                {
                    ItemId = Guid.NewGuid().ToString(),
                    RoleName = extraRole,
                    UserName = user.UserName,
                    UserId = user.ItemId
                };

                _repository.Save(newUserRoleMap);
            }
        }

        public List<PraxisUser> GetControlledUsersForSendingMail(PraxisTraining existingTraining)
        {
            List<PraxisUser> praxisUserList;
            var praxisUserRepo = _mongoDbDataContextProvider.GetTenantDataContext()
                .GetCollection<PraxisUser>("PraxisUsers");
            var filter = Builders<PraxisUser>.Filter.Eq("ClientList.ClientId", existingTraining.ClientId) &
                         Builders<PraxisUser>.Filter.Not(Builders<PraxisUser>.Filter.AnyEq("Roles", RoleNames.GroupAdmin)) &
                         Builders<PraxisUser>.Filter.In("ClientList.Roles", new[] { "mpa-group-1", "mpa-group-2" }) &
                         Builders<PraxisUser>.Filter.Eq("Active", true) &
                         Builders<PraxisUser>.Filter.Eq("IsMarkedToDelete", false);
            if (existingTraining.SpecificControlledMembers.Any())
            {
                praxisUserList = _repository.GetItems<PraxisUser>(u =>
                        existingTraining.SpecificControlledMembers.Contains(u.ItemId) && !u.IsMarkedToDelete)
                    .ToList();
            }
            else
            {
                praxisUserList = praxisUserRepo.Find(filter).ToList();
            }

            return praxisUserList;
        }

        public async Task<EntityQueryResponse<PraxisUser>> GetPraxisUserListReportData(
            string filter, string sort = "{DisplayName: 1}"
        )
        {
            return await _commonUtilService.GetEntityQueryResponse<PraxisUser>(filter, sort);
        }

        public async Task<bool> ProcessPraxisUserDtos(List<string> praxisUserIds)
        {
            var praxisUserDtos = _repository.GetItems<PraxisUserDto>(praxisUserDto =>
                praxisUserIds.Contains(praxisUserDto.PraxisUserId)
            ).ToList();

            try
            {
                var praxisUserIdsWithoutDtos = praxisUserIds.Except(
                    praxisUserDtos.Select(praxisUserDto => praxisUserDto.PraxisUserId)
                ).ToList();
                await CreatePraxisUserDtos(praxisUserIdsWithoutDtos);
                await UpdatePraxisUserDtos(praxisUserDtos);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred while processing praxis users to create/update dtos, {e.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> ProcessPraxisUserDtosForAllPraxisUsers()
        {
            var praxisUsers = (await _commonUtilService.GetEntityQueryResponse<PraxisUser>("{}")).Results;
            var praxisUserIds = praxisUsers.Select(praxisUser => praxisUser.ItemId);
            var filterString = "{PraxisUserId: {$in: [" + string.Join("\",\"", praxisUserIds) + "]}}";
            var praxisUserDtos = (await _commonUtilService.GetEntityQueryResponse<PraxisUserDto>(filterString)).Results;

            try
            {
                var praxisUserIdsWithoutDtos = praxisUserIds.Except(
                    praxisUserDtos.Select(praxisUserDto => praxisUserDto.PraxisUserId)
                ).ToList();
                await CreatePraxisUserDtos(
                    praxisUserIdsWithoutDtos,
                    praxisUsers.Where(praxisUser => praxisUserIdsWithoutDtos.Contains(praxisUser.ItemId)).ToList()
                );
                await UpdatePraxisUserDtos(
                    praxisUserDtos.ToList(),
                    praxisUsers.Where(praxisUser => !praxisUserIdsWithoutDtos.Contains(praxisUser.ItemId)).ToList()
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred while processing praxis users to create/update dtos, {e.Message}");
                return false;
            }

            return true;
        }

        private async Task UpdatePraxisUserDtos(List<PraxisUserDto> praxisUserDtos, List<PraxisUser> praxisUsers = null)
        {
            if (praxisUsers == null)
            {
                var praxisUserIds = praxisUserDtos.Select(praxisUserDto => praxisUserDto.PraxisUserId);
                praxisUsers = _repository.GetItems<PraxisUser>(praxisUser => praxisUserIds.Contains(praxisUser.ItemId))
                    .ToList();
            }

            foreach (var praxisUser in praxisUsers)
            {
                var praxisUserDtoId = praxisUserDtos.Find(praxisUserDto =>
                    praxisUserDto.PraxisUserId.Equals(praxisUser.ItemId)
                ).ItemId;
                await _repository.UpdateAsync(
                    praxisUserDto => praxisUserDto.ItemId.Equals(praxisUserDtoId),
                    _userPersonService.MapPraxisUserDto(praxisUser, praxisUserDtoId)
                );
            }
        }

        private async Task CreatePraxisUserDtos(List<string> praxisUserIds, List<PraxisUser> praxisUsers = null)
        {
            praxisUsers ??= _repository.GetItems<PraxisUser>(praxisUser => praxisUserIds.Contains(praxisUser.ItemId))
                .ToList();
            var praxisUserDtos = praxisUsers.Select(praxisUser =>
                _userPersonService.MapPraxisUserDto(praxisUser, Guid.NewGuid().ToString())
            ).ToList();
            await _repository.SaveAsync(praxisUserDtos);
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {PraxisUser} and {Person} for client {ClientId}", nameof(PraxisUser), nameof(Person), clientId);

            try
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var praxisUsers = Task.Run(() =>
                    _commonUtilService.GetEntityQueryResponse<PraxisUser>(
                        "{\"ClientList.ClientId\": \"" + clientId + "\"}", "{_id: 1}", null,
                        false, 0, 0, false
                        )
                ).Result.Results;

                #region Delete User, PraxisUser, Person

                var praxisUsersToBeDeleted = praxisUsers
                    .Where(praxisUser => praxisUser.ClientList.Count() == 1 || praxisUser.ClientList.Any(c => c.ClientId == clientId && c.IsPrimaryDepartment)).ToList();
                if (praxisUsersToBeDeleted.Any())
                {
                    var praxisUserIdsToBeDeleted = praxisUsersToBeDeleted.Select(pu => pu.ItemId);
                    var userIdsToBeDeleted = praxisUsersToBeDeleted.Select(pu => pu.UserId);

                    var deleteTasks = new List<Task>
                    {
                        _repository.DeleteAsync<PraxisUser>(praxisUser => praxisUserIdsToBeDeleted.Contains(praxisUser.ItemId)),
                        _repository.DeleteAsync<PraxisUserDto>(puDto => userIdsToBeDeleted.Contains(puDto.UserId)),
                        _repository.DeleteAsync<Person>(person => praxisUserIdsToBeDeleted.Contains(person.ItemId)),
                        _repository.DeleteAsync<User>(user => userIdsToBeDeleted.Contains(user.ItemId)),
                        _repository.DeleteAsync<UserRoleMap>(roleMap => userIdsToBeDeleted.Contains(roleMap.UserId)),
                        _repository.DeleteAsync<Connection>(connection =>
                            connection.Tags.Contains("Person-For-User") &&
                            praxisUserIdsToBeDeleted.Contains(connection.ChildEntityID) &&
                            userIdsToBeDeleted.Contains(connection.ParentEntityID)
                        ),
                    };

                    await Task.WhenAll(deleteTasks);
                }

                #endregion

                #region Update PraxisUser

                var praxisUsersToBeUpdated = praxisUsers
                    .Where(praxisUser => praxisUser.ClientList.Count() > 1 && !praxisUser.ClientList.Any(c => c.ClientId == clientId && c.IsPrimaryDepartment));
                foreach (var praxisUser in praxisUsersToBeUpdated)
                {
                    praxisUser.ClientList = praxisUser.ClientList.Where(client => !client.ClientId.Equals(clientId));
                    var isPersonaEnabled = praxisUser.ClientList.Count() > 1 && !praxisUser.Roles.Contains(RoleNames.AdminB);
                    var client = praxisUser.ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment) ?? praxisUser.ClientList.ElementAt(0);
                    praxisUser.ClientId = client?.ClientId;
                    praxisUser.ClientName = client.ClientName;
                    praxisUser.Roles = praxisUser.Roles.Where(role => !role.Contains(clientId));
                    praxisUser.LastUpdateDate = DateTime.Now;
                    praxisUser.LastUpdatedBy = userId;
                    _repository.Update(pu => pu.ItemId.Equals(praxisUser.ItemId), praxisUser);

                    var praxisUserDto = _repository.GetItem<PraxisUserDto>(puDto =>
                        puDto.PraxisUserId.Equals(praxisUser.ItemId)
                    );
                    praxisUserDto.Clients = praxisUserDto.Clients.Where(client => !client.ClientId.Equals(clientId));
                    praxisUserDto.LastUpdateDate = DateTime.Now;
                    praxisUserDto.LastUpdatedBy = userId;
                    _repository.Update(puDto => puDto.ItemId.Equals(praxisUserDto.ItemId), praxisUserDto);

                    var person = _repository.GetItem<Person>(p => p.ItemId.Equals(praxisUser.ItemId));
                    person.Roles = person.Roles.Where(role => !role.Contains(clientId)).ToArray();
                    person.LastUpdateDate = DateTime.Now;
                    person.LastUpdatedBy = userId;
                    _repository.Update(p => p.ItemId.Equals(person.ItemId), person);

                    var user = _repository.GetItem<User>(u => u.ItemId.Equals(praxisUser.UserId));
                    user.Roles = user.Roles.Where(role => !role.Contains(clientId)).ToArray();
                    user.LastUpdateDate = DateTime.Now;
                    user.LastUpdatedBy = userId;
                    user.PersonaEnabled = isPersonaEnabled;
                    _repository.Update(u => u.ItemId.Equals(user.ItemId), user);
                }

                #endregion
                var personaRoleMaps = _repository.GetItems<PersonaRoleMap>(personaRoleMap =>
                    personaRoleMap.PersonaRoles.Any(personaRole => personaRole.RoleName.Contains(clientId))
                );
                var roles = _repository.GetItems<Role>
                        (role => !string.IsNullOrEmpty(role.RoleName) && role.RoleName.Contains(clientId));
                var roleHierarchys = _repository.GetItems<RoleHierarchy>
                        (role => !string.IsNullOrEmpty(role.Role) && role.Role.Contains(clientId));

                if (personaRoleMaps != null && personaRoleMaps.Any())
                {
                    var itemIdsToDelete = personaRoleMaps.Select(i => i.ItemId).ToList();
                    await _repository.DeleteAsync<PersonaRoleMap>(p => itemIdsToDelete.Contains(p.ItemId));
                }
                if (roles != null && roles.Any())
                {
                    var itemIdsToDelete = roles.Select(i => i.ItemId).ToList();
                    await _repository.DeleteAsync<Role>(p => itemIdsToDelete.Contains(p.ItemId));
                }
                if (roleHierarchys != null && roleHierarchys.Any())
                {
                    var itemIdsToDelete = roleHierarchys.Select(i => i.ItemId).ToList();
                    await _repository.DeleteAsync<RoleHierarchy>(p => itemIdsToDelete.Contains(p.ItemId));
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {PraxisUser} and {Person} for client {ClientId}. Error: {Message}. Stacktrace: {StackTrace}", nameof(PraxisUser), nameof(Person), clientId, e.Message, e.StackTrace);
            }
        }
        public async Task<bool> UpdatePraxisUserDto(List<PraxisClientInfo> clientList, string praxisUserId)
        {
            try
            {
                var clientListDto = clientList.Select(cl => new PraxisClientInfoDto
                {
                    ClientId = cl.ClientId,
                    ClientName = cl.ClientName
                });
                var updates = new Dictionary<string, Object>
                {
                    {"Clients", clientListDto}
                };
                _logger.LogInformation("Update PraxisUser dto with praxisUser id -> {PraxisUserId}", praxisUserId);
                await _repository.UpdateAsync<PraxisUserDto>(pud => pud.PraxisUserId == praxisUserId, updates);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in update praxis user dto with error -> {Message}", ex.Message);
                return false;
            }
            return true;
        }

    }
}