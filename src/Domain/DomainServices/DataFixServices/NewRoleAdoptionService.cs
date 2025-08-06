using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class NewRoleAdoptionService : IResolveProdDataIssuesService
    {
        private readonly ILogger<NewRoleAdoptionService> _logger;
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _ecapSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForRole;
        private readonly IChangeLogService _changeLogService;
        private readonly IProcessUserDataByUam _processDataByUamService;
        private readonly INotificationService _notificationProviderService;

        public NewRoleAdoptionService(
            ILogger<NewRoleAdoptionService> logger,
            IRepository repository,
            IMongoSecurityService ecapSecurityService,
            ISecurityContextProvider securityContextProvider,
            IRoleHierarchyForPersonaRoleService roleHierarchyForRole,
            IChangeLogService changeLogService,
            IProcessUserDataByUam processDataByUamService,
            INotificationService notificationProviderService)
        {
            _logger = logger;
            _repository = repository;
            _ecapSecurityService = ecapSecurityService;
            _securityContextProvider = securityContextProvider;
            _roleHierarchyForRole = roleHierarchyForRole;
            _changeLogService = changeLogService;
            _processDataByUamService = processDataByUamService;
            _notificationProviderService = notificationProviderService;
        }

        public Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            _logger.LogInformation("Entered in service: {ServiceName}", nameof(NewRoleAdoptionService));

            var users = GetAllUsers();
            var emails = users.Select(x => x.Email).ToList();
            var personEmails = GetAllPersonsForUsers(emails);
            var praxisUsers = GetAllPraxisUsersForUsers(emails);

            users.ForEach(async u =>
            {
                var isPersonExist = personEmails.Contains(u.Email);
                var praxisUser = FindPraxisUserForUser(praxisUsers, u.Email);
                if (isPersonExist && praxisUser != null)
                {
                    var newRole = PrepareDesiredRole(praxisUser.ClientList?.ToList());
                    if (newRole != null && !u.Roles.ToList().Contains(newRole))
                    {
                        var isRoleCreateSuccess = await CreateRole(newRole, true, RoleNames.Organization_Read_Dynamic);
                        var isPersonaUpdateSuccess = await UpdatePersonaRoles(u.Roles.ToList(), u.ItemId, newRole);
                        if (isRoleCreateSuccess && isPersonaUpdateSuccess)
                        {
                            PrepareRoleUpdates(u, newRole);
                            PersonInformation userInformation = PrepareDataForUamService(u);
                            var isUserUpdateSuccess = await _processDataByUamService.UpdateData(userInformation);
                            if (isUserUpdateSuccess)
                            {
                                var isPraxisUserUpdateSuccess = await UpdatePraxisUser(praxisUser.ItemId, u.Roles.ToList());
                                if (isPraxisUserUpdateSuccess)
                                {
                                    await SendNotificationForLogOutUser(u.ItemId);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("OrganizationId not set for userId: {ItemId}.", u.ItemId);
                    }
                }
                else
                {
                    _logger.LogInformation("Person or PraxisUser data not found for userId: {ItemId}.", u.ItemId);
                }
            });

            _logger.LogInformation("Exiting in service: {ServiceName}", nameof(NewRoleAdoptionService));

            return Task.FromResult(true);
        }

        private List<User> GetAllUsers()
        {
            var adminOrTaskController = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.TaskController };
            return _repository.GetItems<User>(u => !u.IsMarkedToDelete && !u.Roles.Any(r => adminOrTaskController.Contains(r)))?.ToList();
        }

        private List<string> GetAllPersonsForUsers(List<string> emails)
        {
            return _repository.GetItems<Person>(p => emails.Contains(p.Email)).Select(p => p.Email)?.ToList();
        }

        private List<PraxisUser> GetAllPraxisUsersForUsers(List<string> emails)
        {
            return _repository.GetItems<PraxisUser>(pu => emails.Contains(pu.Email))
                .Select(pu =>
                    new PraxisUser
                    {
                        ItemId = pu.ItemId,
                        Email = pu.Email,
                        ClientList = pu.ClientList
                    })
                .ToList();
        }

        private PraxisUser FindPraxisUserForUser(List<PraxisUser> praxisUsers, string email)
        {
            return praxisUsers.Find(pu => pu.Email == email);
        }

        private string PrepareDesiredRole(List<PraxisClientInfo> departmentList)
        {
            var organizationId =
                departmentList != null && departmentList.Count > 0 && !string.IsNullOrWhiteSpace(departmentList[0].ParentOrganizationId) ?
                departmentList[0].ParentOrganizationId :
                null;
            return organizationId != null ? $"{RoleNames.Organization_Read_Dynamic}_{organizationId}" : null;
        }

        private async Task<bool> CreateRole(string role, bool isDynamic, string staticRole)
        {
            _logger.LogInformation("Going to create role: {Role}.", role);
            try
            {
                if (!_ecapSecurityService.IsRoleExist(role))
                {
                    _ecapSecurityService.CreateRole(role, isDynamic);
                }

                var isExistRoleHierarchy = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == role).Result;
                if (!isExistRoleHierarchy)
                {
                    var parents = _roleHierarchyForRole.GetParentList(staticRole);
                    var newRoleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Parents = parents.ToList(),
                        Role = role
                    };
                    await _repository.SaveAsync(newRoleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {RoleHierarchy} entity with ItemId: {ItemId}.",
                        nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while creating new role :{Role}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    role, ex.Message, ex.StackTrace);
                return false;
            }
        }

        private Task<bool> UpdatePersonaRoles(List<string> roles, string userId, string newRole)
        {
            var personaRoles = roles.Where(r => r.Contains("persona"))?.ToList();

            personaRoles.ForEach(async role =>
            {
                var personaRoleMap = await GetPersonaRoleMap(role);
                if (personaRoleMap != null && personaRoleMap.PersonaRoles?.FirstOrDefault(pr => pr.RoleName == newRole) == null)
                {
                    await UpdatePersonaRoleMap(personaRoleMap, newRole);
                }
                else
                {
                    _logger.LogInformation("PersonaRoleMap not found for role: {Role} with userId: {UserId}", role, userId);
                }
            });

            return Task.FromResult(true);
        }

        private async Task<PersonaRoleMap> GetPersonaRoleMap(string persona)
        {
            return await _repository.GetItemAsync<PersonaRoleMap>(prm => prm.Persona == persona);
        }

        private async Task<bool> UpdatePersonaRoleMap(PersonaRoleMap personaRoleMap, string newRole)
        {
            var updates = PreparePersonRoleMapUpdates(personaRoleMap.PersonaRoles.ToList(), newRole);

            var updateFilters = Builders<BsonDocument>.Filter.Eq("_id", personaRoleMap.ItemId);

            return await _changeLogService.UpdateChange("PersonaRoleMap", updateFilters, updates);
        }

        private Dictionary<string, object> PreparePersonRoleMapUpdates(List<PersonaRole> personaRoles, string newRole)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            personaRoles.Add(
                new PersonaRole
                {
                    RoleName = newRole,
                    IsOptional = false
                });

            var updates = new Dictionary<string, object>
            {
                {"PersonaRoles", personaRoles.ToArray()},
                {"LastUpdateDate",  DateTime.UtcNow.ToLocalTime()},
                {"LastUpdatedBy", securityContext.UserId},
            };

            return updates;
        }

        private void PrepareRoleUpdates(User user, string newRole)
        {
            var roles = user.Roles.ToList();
            roles.Add(newRole);
            user.Roles = roles.ToArray();
        }


        private PersonInformation PrepareDataForUamService(User user)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tags = new[] { "is-valid-praxisuser" };
            PersonInformation userInformation = new PersonInformation()
            {
                ItemId = user.ItemId,
                UserId = user.ItemId,
                Salutation = user.Salutation,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                Email = user.Email.ToLower(),
                ProfileImageId = user.ProfileImageId,
                PersonaEnabled = user.PersonaEnabled,
                UserName = user.Email.ToLower(),
                RegisteredBy = 1,
                Password = "",
                CopyEmailTo = new string[] { },
                CountryCode = "",
                DefaultPassword = "",
                Roles = user.Roles.ToArray(),
                TwoFactorEnabled = true,
                Tags = tags,
                Language = securityContext.Language,
                PersonInfo = new Dictionary<string, object>
                {
                    { "Email", user.Email.ToLower()},
                    { "Roles", user.Roles.ToArray() },
                    { "Tags", tags }
                }
            };
            return userInformation;
        }

        private async Task<bool> UpdatePraxisUser(string id, List<string> roles)
        {
            var updates = PreparePraxisUserUpdates(roles);
            await _repository.UpdateAsync<PraxisUser>(pu => pu.ItemId == id, updates);
            return true;
        }

        private Dictionary<string, object> PreparePraxisUserUpdates(List<string> roles)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var updates = new Dictionary<string, object>
            {
                {"Roles", roles},
                {"LastUpdateDate",  DateTime.UtcNow.ToLocalTime()},
                {"LastUpdatedBy", securityContext.UserId},
            };

            return updates;
        }

        private async Task<bool> SendNotificationForLogOutUser(string userId)
        {
            var result = new
            {
                NotifiySubscriptionId = userId,
                Success = true
            };

            await _notificationProviderService.UserLogOutNotification(true, userId, result, "UserUpdate", "RolesUpdated");

            return true;
        }
    }
}