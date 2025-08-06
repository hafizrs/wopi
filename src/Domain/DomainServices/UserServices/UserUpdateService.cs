using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Events;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices
{
    public class UserUpdateService : IProcessUserInformation
    {
        private readonly ILogger<UserUpdateService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IUserRoleService _userRoleService;
        private readonly IProcessUserDataByUam _processDataByUamService;
        private readonly IPraxisUserService _praxisUserService;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversionService;
        private readonly IUserPersonService _userPersonService;
        private readonly IPraxisOrganizationService _praxisOrganizationService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;
        private readonly IServiceClient _serviceClient;

        public UserUpdateService(ILogger<UserUpdateService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IUserRoleService userRoleService,
            IProcessUserDataByUam processDataByUamService,
            IPraxisUserService praxisUserService,
            IDocumentUploadAndConversion documentUploadAndConversionService,
            IUserPersonService userPersonService,
            IPraxisOrganizationService praxisOrganizationService,
            IUserCountMaintainService userCountMaintainService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService,
            IServiceClient serviceClient
        )
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _userRoleService = userRoleService;
            _processDataByUamService = processDataByUamService;
            _praxisUserService = praxisUserService;
            _documentUploadAndConversionService = documentUploadAndConversionService;
            _userPersonService = userPersonService;
            _praxisOrganizationService = praxisOrganizationService;
            _userCountMaintainService = userCountMaintainService;
            _riqsAdminsCreateUpdateService = riqsAdminsCreateUpdateService;
            _serviceClient = serviceClient;
        }
        public Task<bool> ProcessData(PersonInfo userInformation, PraxisClient primaryDepartment, string designation)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ProcessData(PraxisUser praxisUserInformation, FileInformation fileInformation)
        {
            _logger.LogInformation("Going to process new user data with email: {Email}.", praxisUserInformation.Email);
            try
            {
                var existingPraxisUser = _repository.GetItem<PraxisUser>(p => p.Email == praxisUserInformation.Email);
                if (existingPraxisUser != null)
                {
                    _logger.LogInformation("Existing user client count -> {ClientCount}", existingPraxisUser.ClientList.Count());
                    var roles = PrepareUserRoles(praxisUserInformation.ClientList.ToList(), praxisUserInformation.Roles.ToList());
                    praxisUserInformation.Roles = praxisUserInformation.Roles.Concat(roles.ToArray());
                    praxisUserInformation.Roles = praxisUserInformation.Roles.Distinct();
                    bool isUpdated = await ProcessUserUpdate(praxisUserInformation, fileInformation);
                    if (isUpdated)
                    {
                        await UpdateDepartmentUserCount(existingPraxisUser.ClientList.ToList(), praxisUserInformation.ClientList.ToList());
                        await UpdateOrganization(praxisUserInformation.Email, praxisUserInformation.Roles.ToList(), existingPraxisUser.Roles.ToList());
                        await UpdateReportingDependencies(existingPraxisUser);

                    }
                    return isUpdated;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in User UpdateService: {ErrorMessage}.", ex.Message);
            }
            return true;
        }

        private async Task UpdateOrganization(string email, List<string> roles, List<string> previousRoles)
        {
            var adminRoleBefore = previousRoles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));
            var adminRoleAfter = roles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));

            if (previousRoles.Contains(RoleNames.GroupAdmin))
            {
                adminRoleBefore = null;
            }
            if (roles.Contains(RoleNames.GroupAdmin))
            {
                adminRoleAfter = null;
            }

            string userStatus = string.Empty;
            string adminBDynamicRole = string.Empty;

            if (adminRoleBefore != null && adminRoleAfter == null)
            {
                userStatus = "Removed";
                adminBDynamicRole = adminRoleBefore;
            }
            else if (adminRoleBefore == null && adminRoleAfter != null)
            {
                userStatus = "Created";
                adminBDynamicRole = adminRoleAfter;
            }

            if (!string.IsNullOrEmpty(userStatus))
            {
                string orgId = adminBDynamicRole.Split('_')[1];
                await _praxisOrganizationService.UpdateOrganizationAdminIds(orgId, email, userStatus);
            }
        }

        private async Task UpdateDepartmentUserCount(
            List<PraxisClientInfo> oldDepartmentList,
            List<PraxisClientInfo> newDepartmentList)
        {
            var newDepartmentIds = newDepartmentList.Select(x => x.ClientId).ToList();
            var oldDepartmentIds = oldDepartmentList.Select(x => x.ClientId).ToList();

            var deletedDepartmentList = oldDepartmentList.Where(oc => !newDepartmentIds.Contains(oc.ClientId)).ToList();
            var addedDepartmentList = newDepartmentList.Where(nc => !oldDepartmentIds.Contains(nc.ClientId)).ToList();

            var deletedPrimaryDepartment = GetPrimaryDepartmentId(deletedDepartmentList);
            var addedPrimaryDepartment = GetPrimaryDepartmentId(addedDepartmentList);

            if (deletedPrimaryDepartment != null)
            {
                await _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(deletedPrimaryDepartment.ClientId, deletedPrimaryDepartment.ParentOrganizationId);
            }
            if (addedPrimaryDepartment != null)
            {
                await _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(addedPrimaryDepartment.ClientId, addedPrimaryDepartment.ParentOrganizationId);
            }
        }

        private async Task UpdateReportingDependencies(PraxisUser praxisUser)
        {
            var permissions = _repository.GetItems<CirsDashboardPermission>(c => c.AdminIds != null && c.AdminIds.Any(a => a.UserId == praxisUser.UserId)).ToList();
            var isAAdminBUser = IsAAdminBUser(praxisUser?.Roles?.ToList() ?? new List<string>());
            foreach (var permission in permissions)
            {
                var isAInvalidAdmin = praxisUser?.ClientList?.Any(c => c.ClientId == permission.PraxisClientId) != true;
                if (!isAInvalidAdmin)
                {
                    isAInvalidAdmin = isAAdminBUser && (permission.AssignmentLevel != AssignmentLevel.Organizational ||
                        (permission.AssignmentLevel == AssignmentLevel.Organizational && permission.CirsDashboardName == CirsDashboardName.Hint));
                }

                if (isAInvalidAdmin)
                {
                    permission.AdminIds = permission.AdminIds.Where(a => a.UserId != praxisUser.UserId);
                    await _repository.UpdateAsync(p => p.ItemId == permission.ItemId, permission);

                    var cirsAdminAssignedEvent = new GenericEvent
                    {
                        EventType = PraxisEventType.CirsAdminAssignedEvent,
                        JsonPayload = JsonConvert.SerializeObject(permission.ItemId)
                    };

                    _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), cirsAdminAssignedEvent);
                }
            }
        }

        private PraxisClientInfo GetPrimaryDepartmentId(IEnumerable<PraxisClientInfo> clientList)
        {
            return clientList.FirstOrDefault(c => c.IsPrimaryDepartment);
        }

        private List<string> PrepareUserRoles(List<PraxisClientInfo> clientList, List<string> existingRoles)
        {
            List<string> userRoles = new List<string>();
            bool isRQMonitorClient = clientList.Select(x => x.ClientId == PraxisConstants.RQMonitorClientId).FirstOrDefault();

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

            foreach (var client in clientList)
            {
                userRoles = userRoles.Concat(client.Roles).ToList();
            }

            userRoles = userRoles.Distinct().ToList();

            return userRoles;
        }

        private bool CanBeAdminBUser(List<string> existingRoles)
        {
            return existingRoles.Contains(RoleNames.AdminB);
        }

        private bool CanBeAdminAminusUser(List<string> existingRoles)
        {
            return existingRoles.Contains(RoleNames.GroupAdmin);
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

        private async Task<bool> ProcessUserUpdate(PraxisUser praxisUserInformation, FileInformation fileInformation)
        {
            _logger.LogInformation("Going to process new user data to update with email: {Email}.", praxisUserInformation.Email);
            try
            {
                PersonInformation userInformation = PrepareDataForUamService(praxisUserInformation, praxisUserInformation.ItemId);
                praxisUserInformation.Roles = userInformation.Roles;
                var response = await _processDataByUamService.UpdateData(userInformation);
                if (response)
                {
                    if (fileInformation != null && !string.IsNullOrEmpty(fileInformation.FileName) && !string.IsNullOrEmpty(fileInformation.FileId))
                    {
                        var fileId = fileInformation.FileId;

                        var success = await UpdateDataToPraxisUserTable(praxisUserInformation, userInformation.ItemId, fileId, fileInformation.FileName);
                        if (success)
                        {
                            await _documentUploadAndConversionService.FileConversion(fileId, TagName.ProfileImageOfPerson);
                        }
                    }
                    else
                    {
                        await UpdateDataToPraxisUserTable(praxisUserInformation, userInformation.ItemId);
                    }
                }
                else
                {
                    _logger.LogError("User update failed for email: {Email}", userInformation.Email);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in ProcessCreateUser of UserUpdateService for email: {Email}, error: {ErrorMessage}", praxisUserInformation.Email, ex.Message);
                return false;
            }
        }

        private async Task<bool> UpdateDataToPraxisUserTable(PraxisUser praxisUserInformation, string userId, string fileId = null, string fileName = null)
        {
            try
            {
                var existingPerson = _repository.GetItem<Person>(p => p.Email == praxisUserInformation.Email);
                if (existingPerson != null)
                {
                    PraxisUser mappedPraxisUserInformation = MapPraxisUserData(praxisUserInformation, existingPerson, existingPerson.ItemId, userId, fileId, fileName);
                    await _repository.UpdateAsync(x => x.ItemId == mappedPraxisUserInformation.ItemId, mappedPraxisUserInformation);
                    await ProcessPraxisUSerRole(mappedPraxisUserInformation);
                    var existingPraxisUserDto = _repository.GetItem<PraxisUserDto>(x => x.UserId == mappedPraxisUserInformation.UserId);
                    if (existingPraxisUserDto != null)
                    {
                        PraxisUserDto praxisUserDtoInfo = _userPersonService.MapPraxisUserDto(mappedPraxisUserInformation, existingPraxisUserDto.ItemId);
                        await _repository.UpdateAsync(x => x.ItemId == praxisUserDtoInfo.ItemId, praxisUserDtoInfo);
                    }
                    await _riqsAdminsCreateUpdateService.CreateUpdateRiqsGroupAdmin(mappedPraxisUserInformation);
                    SendEmailOnUpdatePraxisUser(mappedPraxisUserInformation);
                    _logger.LogInformation("PraxisUser inserted with userId -> {UserId}", mappedPraxisUserInformation.UserId);
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

        private void SendEmailOnUpdatePraxisUser(PraxisUser praxisUser)
        {
            var latestOrganizationId = praxisUser.ClientList.Where(x => x.IsLatest).Select(y => y.ClientId).FirstOrDefault();
            if (!string.IsNullOrEmpty(latestOrganizationId))
            {
                _userPersonService.SendEmailToUserForLatestClient(praxisUser);
            }
        }

        private PraxisUser MapPraxisUserData(PraxisUser praxisUserInformation, Person personData, string personId, string userId, string fileId = null, string fileName = null)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            praxisUserInformation.CreatedBy = personData.CreatedBy;
            praxisUserInformation.LastUpdatedBy = !string.IsNullOrEmpty(securityContext.UserId) ? securityContext.UserId : personData.CreatedBy;
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
            praxisUserInformation.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
            praxisUserInformation.TenantId = securityContext.TenantId;
            praxisUserInformation.Active = personData.Active;
            if (fileId != null)
                praxisUserInformation.Image = new PraxisImage
                {
                    FileId = fileId,
                    FileName = fileName
                };

            return praxisUserInformation;
        }

        private Task<bool> ProcessPraxisUSerRole(PraxisUser praxisUserInformation)
        {
            try
            {
                string praxisUserId = praxisUserInformation.ItemId;

                if (string.IsNullOrEmpty(praxisUserId)) return Task.FromResult(false);

                if (praxisUserInformation.ClientList != null || praxisUserInformation.ClientList.Any())
                {
                    var clientIds = praxisUserInformation.ClientList.Select(p => p.ClientId).ToList();
                    if (clientIds.Count == 1)
                    {
                        var praxisClient = _repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(clientIds[0]) && !pc.IsMarkedToDelete);

                        if (praxisClient?.CompanyTypes != null && praxisClient.CompanyTypes.ToList().Exists(c => c.Equals(RoleNames.TechnicalClient)))
                        {
                            _logger.LogInformation("PraxisUser.Created, Praxis User");
                            _praxisUserService.RoleAssignToPraxisUser(praxisUserId, praxisUserInformation.ClientList, true);
                        }
                        else
                        {
                            _logger.LogInformation("PraxisUser.Created, Non Praxis User");
                            _praxisUserService.RoleAssignToPraxisUser(praxisUserId, praxisUserInformation.ClientList);
                        }

                        _praxisUserService.AddRowLevelSecurity(praxisUserId, new[] { praxisClient?.ItemId });
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
                _logger.LogInformation("Handled by {HandlerName} with praxisUserId: {PraxisUserId}", nameof(ProcessPraxisUSerRole), praxisUserInformation.ItemId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during insert dynamic role while creating new {EntityName} with ItemId: {ItemId}. Exception message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(PraxisUser), praxisUserInformation.ItemId, ex.Message, ex.StackTrace);
            }

            return Task.FromResult(false);
        }

        private PersonInformation PrepareDataForUamService(PraxisUser praxisUserInformation, string userId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tags = new[] { "is-valid-praxisuser" };
            PersonInformation userInformation = new PersonInformation()
            {
                ItemId = userId,
                UserId = userId,
                Salutation = praxisUserInformation.Salutation,
                FirstName = praxisUserInformation.FirstName,
                LastName = praxisUserInformation.LastName,
                DisplayName = praxisUserInformation.DisplayName,
                Email = praxisUserInformation.Email.ToLower(),
                PersonaEnabled = IsPersonaEnabled(praxisUserInformation),
                CountryCode = "",
                Roles = praxisUserInformation.Roles.ToArray(),
                TwoFactorEnabled = false,
                Tags = tags,
                Language = securityContext.Language,
                PhoneNumber = praxisUserInformation.Phone
            };
            return userInformation;
        }

    }

}
