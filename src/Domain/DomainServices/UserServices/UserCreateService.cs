using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices
{
    public class UserCreateService : IProcessUserInformation
    {
        private readonly IUserRoleService _userRoleService;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversion;
        private readonly ILogger<UserCreateService> _logger;
        private readonly IPraxisUserService _praxisUserService;
        private readonly IProcessUserDataByUam _processDataByUamService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IUserPersonService _userPersonService;
        private readonly DeleteDataForUser _deleteDataForUser;
        private readonly IPraxisOrganizationService _praxisOrganizationService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;

        public UserCreateService(
            IProcessUserDataByUam processDataByUamService,
            ISecurityContextProvider securityContextProvider,
            IDocumentUploadAndConversion documentUploadAndConversion,
            ILogger<UserCreateService> logger,
            IPraxisUserService praxisUserService,
            IRepository repository,
            IUserRoleService userRoleService,
            IUserPersonService userPersonService,
            DeleteDataForUser deleteDataForUser,
            IPraxisOrganizationService praxisOrganizationService,
            IUserCountMaintainService userCountMaintainService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService)
        {
            _repository = repository;
            _processDataByUamService = processDataByUamService;
            _securityContextProvider = securityContextProvider;
            _documentUploadAndConversion = documentUploadAndConversion;
            _logger = logger;
            _praxisUserService = praxisUserService;
            _userRoleService = userRoleService;
            _userPersonService = userPersonService;
            _deleteDataForUser = deleteDataForUser;
            _praxisOrganizationService = praxisOrganizationService;
            _userCountMaintainService = userCountMaintainService;
            _riqsAdminsCreateUpdateService= riqsAdminsCreateUpdateService;
        }

        public Task<bool> ProcessData(PersonInfo userInformation, PraxisClient primaryDepartment, string designation)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ProcessData(PraxisUser praxisUserInformation, FileInformation fileInformation)
        {
            _logger.LogInformation("Start user create processing for email id: {EmailId}", praxisUserInformation.Email);
            try
            {
                var roles = PrepareUserRoles(praxisUserInformation.ClientList.ToList(), praxisUserInformation.Roles.ToList());
                praxisUserInformation.Roles = praxisUserInformation.Roles.Concat(roles.ToArray()).ToList();
                praxisUserInformation.Roles = praxisUserInformation.Roles.Distinct();
                var isCreated = await ProcessCreateUser(praxisUserInformation, fileInformation);
                if (isCreated)
                {
                    await UpdateDepartmentUserCount(praxisUserInformation.ClientList);
                    await UpdateOrganization(praxisUserInformation.Email, praxisUserInformation.Roles.ToList());
                }
                return isCreated;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in ProcessData of UserCreateService for email: {Email}. Error: {ErrorMessage}.", praxisUserInformation.Email, ex.Message);
                return false;
            }
        }

        private async Task<bool> ProcessCreateUser(PraxisUser praxisUserInformation, FileInformation fileInformation)
        {
            _logger.LogInformation("Going to process new user data to create with email: {Email}.", praxisUserInformation.Email);

            try
            {
                var userInformation = PrepareDataForUamService(praxisUserInformation);
                praxisUserInformation.Roles = userInformation.Roles;
                var (item1, userId) = await _processDataByUamService.SaveData(userInformation);
                if (item1)
                {
                    if (fileInformation != null && !string.IsNullOrEmpty(fileInformation.FileName) &&
                        !string.IsNullOrEmpty(fileInformation.FileId))
                    {
                        var fileId = fileInformation.FileId;

                        var success = await SaveDataToPraxisUserTable(praxisUserInformation, userId, fileId, fileInformation.FileName);
                        if (success)
                        {
                            await _documentUploadAndConversion.FileConversion(fileId, TagName.ProfileImageOfPerson);
                        }
                    }
                    else
                    {
                        await SaveDataToPraxisUserTable(praxisUserInformation, userId);
                    }
                }
                else
                {
                    _logger.LogError("User creation failed for email: {Email}", userInformation.Email);
                    _deleteDataForUser.DeleteData("", userInformation.UserId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in ProcessCreateUser of UserCreateService for email: {Email}. Error: {ErrorMessage}.", praxisUserInformation.Email, ex.Message);
                return false;
            }
        }

        private async Task UpdateOrganization(string email, List<string> roles)
        {
            var adminBDynamicRole = roles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));
            if (!string.IsNullOrEmpty(adminBDynamicRole))
            {
                string orgId = adminBDynamicRole.Split('_')[1];
                await _praxisOrganizationService.UpdateOrganizationAdminIds(orgId, email, "Created");
            }
        }

        private async Task UpdateDepartmentUserCount(IEnumerable<PraxisClientInfo> clientList)
        {
            var primaryDepartment = GetPrimaryDepartment(clientList);
            if (primaryDepartment != null)
            {
                await _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(primaryDepartment.ClientId, primaryDepartment.ParentOrganizationId);
            }
        }

        private PraxisClientInfo GetPrimaryDepartment(IEnumerable<PraxisClientInfo> clientList)
        {
            return clientList.FirstOrDefault(c => c.IsPrimaryDepartment);
        }

        private async Task<bool> SaveDataToPraxisUserTable(PraxisUser praxisUserInformation, string userId,
            string fileId = null, string fileName = null)
        {
            try
            {
                var existingPerson = _repository.GetItem<Person>(p => p.Email == praxisUserInformation.Email);
                if (existingPerson != null)
                {
                    var mappedPraxisUserInformation = MapPraxisUserData(praxisUserInformation, existingPerson,
                        existingPerson.ItemId, userId, fileId, fileName);
                    await _repository.SaveAsync(mappedPraxisUserInformation);
                    await ProcessPraxisUserRole(mappedPraxisUserInformation);
                    PraxisUserDto praxisUserDtoInfo = _userPersonService.MapPraxisUserDto(mappedPraxisUserInformation);
                    await _repository.SaveAsync(praxisUserDtoInfo);
                    await _riqsAdminsCreateUpdateService.CreateUpdateRiqsGroupAdmin(mappedPraxisUserInformation);
                }
                else
                {
                    _logger.LogError("No person data found for userId: {UserId}", userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in Praxis user creation for userId: {UserId}. Exception: {ErrorMessage}", praxisUserInformation.UserId, ex.Message);
            }

            return true;
        }

        private Task<bool> ProcessPraxisUserRole(PraxisUser praxisUserInformation)
        {
            try
            {
                var praxisUserId = praxisUserInformation.ItemId;

                if (string.IsNullOrEmpty(praxisUserId)) return Task.FromResult(false);

                if (praxisUserInformation.ClientList != null || praxisUserInformation.ClientList.Any())
                {
                    var clientIds = praxisUserInformation.ClientList.Select(p => p.ClientId).ToList();
                    if (clientIds.Count == 1)
                    {
                        var praxisClient = _repository.GetItem<PraxisClient>(pc =>
                            pc.ItemId.Equals(clientIds[0]) && !pc.IsMarkedToDelete);

                        if (praxisClient?.CompanyTypes != null && praxisClient.CompanyTypes.ToList()
                            .Exists(c => c.Equals(RoleNames.TechnicalClient)))
                        {
                            _logger.LogInformation("PraxisUser.Created, Praxis User");
                            _praxisUserService.RoleAssignToPraxisUser(praxisUserId, praxisUserInformation.ClientList,
                                true);
                        }
                        else
                        {
                            _logger.LogInformation("PraxisUser.Created, Non Praxis User");
                            _praxisUserService.RoleAssignToPraxisUser(praxisUserId, praxisUserInformation.ClientList);
                        }

                        _praxisUserService.AddRowLevelSecurity(praxisUserId, new[] {praxisClient?.ItemId});
                    }
                    else if (clientIds.Count > 1)
                    {
                        _logger.LogInformation("PraxisUser.Created, Non Praxis User");
                        _praxisUserService.RoleAssignToPraxisUser(praxisUserId, praxisUserInformation.ClientList);
                        _praxisUserService.AddRowLevelSecurity(praxisUserId, clientIds.ToArray());
                    }
                }
                else
                {
                    _logger.LogInformation("ClientId missing for PraxisUserId: {PraxisUserId}", praxisUserId);
                }

                _logger.LogInformation("Handled by {HandlerName} with praxisUserId: {PraxisUserId}", nameof(ProcessPraxisUserRole), praxisUserInformation.ItemId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during insert dynamic role while creating new {EntityName} with ItemId: {ItemId}. Exception message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(PraxisUser), praxisUserInformation.ItemId, ex.Message, ex.StackTrace);
            }

            return Task.FromResult(false);
        }

        private PersonInformation PrepareDataForUamService(PraxisUser praxisUserInformation)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tags = new[] {"is-valid-praxisuser"};
            var userInformation = new PersonInformation
            {
                ItemId = praxisUserInformation.ItemId,
                UserId = praxisUserInformation.ItemId,
                Salutation = praxisUserInformation.Salutation,
                FirstName = praxisUserInformation.FirstName,
                LastName = praxisUserInformation.LastName,
                DisplayName = praxisUserInformation.DisplayName,
                Email = praxisUserInformation.Email.ToLower(),
                ProfileImageId = praxisUserInformation.Image.FileId,
                PersonaEnabled = IsPersonaEnabled(praxisUserInformation),
                UserName = praxisUserInformation.Email.ToLower(),
                RegisteredBy = 1,
                Password = "",
                CopyEmailTo = new string[] { },
                CountryCode = "",
                DefaultPassword = "",
                Roles = praxisUserInformation.Roles.ToArray(),
                TwoFactorEnabled = true,
                Tags = tags,
                Language = securityContext.Language,
                PersonInfo = new Dictionary<string, object>
                {
                    {"FirstName", praxisUserInformation.FirstName},
                    {"LastName", praxisUserInformation.LastName},
                    {"DisplayName", praxisUserInformation.DisplayName},
                    {"Email", praxisUserInformation.Email.ToLower()},
                    {"Roles", praxisUserInformation.Roles},
                    {"PhoneNumber", praxisUserInformation.Telephone},
                    {"Tags", tags},
                    {"Salutation", praxisUserInformation.Salutation},
                    {"ProfileImageId", praxisUserInformation.Image.FileId},
                    {"OrganizationId", praxisUserInformation.ClientId},
                    {"Organization", praxisUserInformation.ClientName},
                    {"OrganizationNames", new[] {praxisUserInformation.ClientName}}
                }
            };
            return userInformation;
        }

        private PraxisUser MapPraxisUserData(PraxisUser praxisUserInformation, Person personData, string personId,
            string userId, string fileId = null, string fileName = null)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            praxisUserInformation.IdsAllowedToRead = personData.IdsAllowedToRead;
            praxisUserInformation.IdsAllowedToUpdate = personData.IdsAllowedToUpdate;
            praxisUserInformation.IdsAllowedToDelete = personData.IdsAllowedToDelete;
            praxisUserInformation.Roles = personData.Roles;
            praxisUserInformation.ItemId = personId;
            praxisUserInformation.UserId = userId;
            praxisUserInformation.RolesAllowedToRead = personData.RolesAllowedToRead;
            praxisUserInformation.RolesAllowedToUpdate = personData.RolesAllowedToUpdate;
            praxisUserInformation.RolesAllowedToDelete = personData.RolesAllowedToDelete;
            praxisUserInformation.IsMarkedToDelete = false;
            praxisUserInformation.CreateDate = DateTime.UtcNow.ToLocalTime();
            praxisUserInformation.CreatedBy = personData.CreatedBy;
            praxisUserInformation.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
            praxisUserInformation.LastUpdatedBy = securityContext.UserId;
            praxisUserInformation.TenantId = securityContext.TenantId;
            if (fileId != null)
                praxisUserInformation.Image = new PraxisImage
                {
                    FileId = fileId,
                    FileName = fileName
                };
            return praxisUserInformation;
        }

        private List<string> PrepareUserRoles(List<PraxisClientInfo> clientList, List<string> existingRoles)
        {
            var userRoles = new List<string>();
            var isRQMonitorClient = clientList.Select(x => x.ClientId == "d1ca7172-2120-4eb2-a7af-a00fd99fdbe2")
                .FirstOrDefault();

            userRoles.Add(RoleNames.AppUser);
            userRoles.Add(RoleNames.Anonymous);
            userRoles.AddRange(_userRoleService.GetOrganizationWideRoles(clientList));

            if (CanBeAdminAminusUser(existingRoles) && !isRQMonitorClient)
            {
                userRoles.AddRange(_userRoleService.PrepareAdminBRoles(clientList, true));
            }
            else if (CanBeAdminBUser(existingRoles) && !isRQMonitorClient) 
            {
                userRoles.AddRange(_userRoleService.PrepareAdminBRoles(clientList));
            }
            else if (clientList.Count > 1 && !isRQMonitorClient)
            {
                var personaRoles = _userRoleService.GetPersonaRoles(clientList);
                userRoles.AddRange(personaRoles);
                userRoles.Add("client_specific");
            }
            else if (clientList.Count == 1 && !isRQMonitorClient)
            {
                var isPowerUserRoleExist = clientList.Select(x => x.Roles.Contains("poweruser")).FirstOrDefault();
                var clientId = clientList.Select(x => x.ClientId).FirstOrDefault();
                if (isPowerUserRoleExist)
                {
                    var deleteRoles = _userRoleService.GetDeleteFeatureRole(clientId);
                    userRoles.Add(deleteRoles);
                }

                userRoles.Add("client_specific");
            }
            else
            {
                userRoles.Add("technical_client");
            }

            foreach (var client in clientList) userRoles = userRoles.Concat(client.Roles).ToList();

            userRoles = userRoles.Distinct().ToList();

            return userRoles;
        }

        private bool CanBeAdminAminusUser(List<string> existingRoles)
        {
            return existingRoles.Contains(RoleNames.GroupAdmin);
        }

        private bool CanBeAdminBUser(List<string> existingRoles)
        {
            return existingRoles.Contains(RoleNames.AdminB);
        }

        private bool IsPersonaEnabled(PraxisUser praxisUserInformation)
        {
            var isPersonaEnabled = true;
            if (IsAAdminBUser(praxisUserInformation.Roles.ToList()))
            {
                isPersonaEnabled = false;
            }
            else if (praxisUserInformation.ClientList.Count() == 1)
            {
                isPersonaEnabled = false;
            }
            return isPersonaEnabled;
        }

        private bool IsAAdminBUser(List<string> roles)
        {
            return roles.Contains(RoleNames.AdminB);
        }

        private List<PraxisClient> GetAllDepartmentList(string orgId)
        {
            return _repository.GetItems<PraxisClient>(p => p.ParentOrganizationId == orgId).ToList();
        }
    }
}