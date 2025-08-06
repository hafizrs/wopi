using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class ProcessExistingUserDataService : IProcessUserInformation
    {
        private readonly ILogger<ProcessExistingUserDataService> _logger;
        private readonly IRepository _repository;
        private readonly IProcessUserDataByUam _processUserDataByUamService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversion;
        private readonly IUpdateDeletePermissionForOpenOrg _updateDeletePermissionForOpenOrg;
        private readonly IPrepareNewRole _prepareNewRoleService;
        private readonly IUserPersonService _userPersonService;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IPraxisOrganizationService _praxisOrganizationService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;

        public ProcessExistingUserDataService(
            ILogger<ProcessExistingUserDataService> logger,
            IRepository repository,
            IProcessUserDataByUam processUserDataByUamService,
            ISecurityContextProvider securityContextProvider,
            IMongoSecurityService mongoSecurityService,
            IDocumentUploadAndConversion documentUploadAndConversion,
            IUpdateDeletePermissionForOpenOrg updateDeletePermissionForOpenOrg,
            IPrepareNewRole prepareNewRoleService,
            IUserPersonService userPersonService,
            IPraxisClientService praxisClientService,
            IPraxisOrganizationService praxisOrganizationService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService)
        {
            _logger = logger;
            _repository = repository;
            _processUserDataByUamService = processUserDataByUamService;
            _securityContextProvider = securityContextProvider;
            _mongoSecurityService = mongoSecurityService;
            _documentUploadAndConversion = documentUploadAndConversion;
            _updateDeletePermissionForOpenOrg = updateDeletePermissionForOpenOrg;
            _prepareNewRoleService = prepareNewRoleService;
            _userPersonService = userPersonService;
            _praxisClientService = praxisClientService;
            _praxisOrganizationService = praxisOrganizationService;
            _riqsAdminsCreateUpdateService = riqsAdminsCreateUpdateService;
        }

        public Task<bool> ProcessData(PraxisUser praxisUserInformation, FileInformation fileInformation)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ProcessData(PersonInfo userInformation, PraxisClient primaryDepartment, string designation)
        {
            _logger.LogInformation("Going to process new user data with email: {Email}.", userInformation.PersonalInformation.Email);
            try
            {
                var fileId = !string.IsNullOrEmpty(userInformation.FileName) && !string.IsNullOrEmpty(userInformation.FileId) ? userInformation.FileId : null;
                var existingPraxisUser = _repository.GetItem<PraxisUser>(p => p.Email == userInformation.PersonalInformation.Email);
                if (existingPraxisUser != null)
                {
                    var departments = GetDepartments(primaryDepartment.ParentOrganizationId);
                    var departmentIds = departments.Select(x => x.ItemId).ToList();

                    var praxisUserUpdate = await UpdateDataToPraxisUserTable(
                        userInformation.PersonalInformation,
                        primaryDepartment,
                        departments,
                        departmentIds,
                        existingPraxisUser,
                        designation,
                        userInformation.IsGroupAdmin,
                        fileId,
                        userInformation.FileName);
                    if (praxisUserUpdate)
                    {
                        var userInfo = PrepareUserDataForUamService(
                            userInformation.PersonalInformation,
                            existingPraxisUser,
                            primaryDepartment,
                            departments,
                            departmentIds,
                            userInformation.IsGroupAdmin);
                        var userUpdate = await _processUserDataByUamService.UpdateData(userInfo);
                        if (userUpdate)
                        {
                            await _updateDeletePermissionForOpenOrg.UpdatePermission(primaryDepartment.ItemId, true);
                            if (fileId != null)
                            {
                                await _documentUploadAndConversion.FileConversion(fileId, TagName.ProfileImageOfPerson);
                            }
                            await UpdateOrganization(userInfo.Email, userInfo.Roles.ToList(), existingPraxisUser.Roles.ToList());
                        }
                    }

                    await _userPersonService.PrepareDataForFeatureRoleMap($"{RoleNames.PowerUser_Dynamic}_{primaryDepartment.ItemId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during processing existing user data with email: {Email}. Exception Message: {Message}. Exception Details: {StackTrace}.", userInformation.PersonalInformation.Email, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task UpdateOrganization(string email, List<string> roles, List<string> previousRoles)
        {
            var adminRoleBefore = previousRoles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));
            var adminRoleAfter = roles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));

            if (adminRoleBefore == null && adminRoleAfter != null)
            {
                string orgId = adminRoleAfter.Split('_')[1];
                await _praxisOrganizationService.UpdateOrganizationAdminIds(orgId, email, "Created");
            }
        }

        private async Task<bool> UpdateDataToPraxisUserTable(
            PersonInformation personInformation,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            PraxisUser praxisUser,
            string designation,
            bool isGroupAdmin,
            string fileId = null,
            string fileName = null)
        {
            try
            {
                var existingPraxisUser = MapPraxisUserData(
                    personInformation,
                    primaryDepartment,
                    departments,
                    departmentIds,
                    praxisUser,
                    isGroupAdmin);
                if (fileId != null)
                {
                    existingPraxisUser.Image = new PraxisImage
                    {
                        FileId = fileId,
                        FileName = fileName
                    };
                }
                await _repository.UpdateAsync<PraxisUser>(p => p.ItemId == existingPraxisUser.ItemId, existingPraxisUser);
                var existingPraxisUserDto = _repository.GetItem<PraxisUserDto>(x => x.UserId == existingPraxisUser.UserId);
                if (existingPraxisUserDto != null)
                {
                    PraxisUserDto praxisUserDtoInfo = _userPersonService.MapPraxisUserDto(existingPraxisUser, existingPraxisUserDto.ItemId);
                    await _repository.UpdateAsync<PraxisUserDto>(x => x.ItemId == praxisUserDtoInfo.ItemId, praxisUserDtoInfo);
                }
                await _praxisClientService.UpdateClientPaymentUserData(primaryDepartment.ItemId, existingPraxisUser.ItemId, designation);
                await _riqsAdminsCreateUpdateService.CreateUpdateRiqsGroupAdmin(existingPraxisUser);
                _logger.LogInformation("User data has been successfully updated in {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisUser), existingPraxisUser.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during updating to {EntityName} entity with ItemId: {ItemId}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisUser), praxisUser.ItemId, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private PraxisUser MapPraxisUserData(
            PersonInformation personInformation,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            PraxisUser existingPraxisUser,
            bool isGroupAdmin)
        {
            List<string> roles = GetUserRoles(primaryDepartment, departments, departmentIds, isGroupAdmin);

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

            var clientList = GetPraxisUserClientList(departments, primaryDepartment);

            existingPraxisUser.RolesAllowedToRead = rolesAllowToRead.ToArray();
            existingPraxisUser.RolesAllowedToUpdate = rolesAllowToUpdate.ToArray();
            existingPraxisUser.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
            existingPraxisUser.FirstName = personInformation.FirstName;
            existingPraxisUser.LastName = personInformation.LastName;
            existingPraxisUser.DisplayName = personInformation.DisplayName;
            existingPraxisUser.Salutation = personInformation.Salutation;
            existingPraxisUser.Gender = personInformation.Gender;
            existingPraxisUser.DateOfBirth = personInformation.DateOfBirth;
            existingPraxisUser.Nationality = personInformation.Nationality;
            existingPraxisUser.MotherTongue = personInformation.MotherTongue;
            existingPraxisUser.OtherLanguage = personInformation.OtherLanguage;
            existingPraxisUser.ClientList = clientList;
            existingPraxisUser.Roles = roles.ToArray();
            existingPraxisUser.Skills = new string[] { };
            existingPraxisUser.Specialities = new PraxisSpeciality[] { };

            return existingPraxisUser;
        }

        private PersonInformation PrepareUserDataForUamService(
            PersonInformation personInformation,
            PraxisUser existingPraxisUser,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            bool isGroupAdmin)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var roles = GetUserRoles(primaryDepartment, departments, departmentIds, isGroupAdmin).ToArray();

            personInformation.Roles = roles.ToArray();
            personInformation.UserId = existingPraxisUser.UserId;
            personInformation.MotherTongue = securityContext.Language;
            personInformation.PersonaEnabled = false;
            personInformation.TwoFactorEnabled = true;
            personInformation.PersonInfo = new Dictionary<string, object>
                {
                    { "FirstName", personInformation.FirstName},
                    { "LastName", personInformation.LastName },
                    { "DisplayName", personInformation.DisplayName },
                    { "Email", personInformation.Email.ToLower()},
                    { "Salutation", personInformation.Salutation},
                    { "ProfileImageId", personInformation.ProfileImageId},
                    //{ "OrganizationId", primaryDepartment.ItemId},
                    //{ "Organization", primaryDepartment.ClientName},
                    //{ "OrganizationNames", personInformation.OrganizationNames }
                };

            return personInformation;
        }

        private List<string> GetUserRoles(
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            bool isGroupAdmin)
        {
            var adminBRole = $"{RoleNames.AdminB_Dynamic}_{primaryDepartment.ParentOrganizationId}";
            var paymentRole = _prepareNewRoleService.SaveRole(
                $"{RoleNames.PoweruserPayment}_{primaryDepartment.ItemId}",
                primaryDepartment.ItemId,
                RoleNames.PowerUser,
                true);
            var clientAdminAccessRoles = GetClientAdminAccessRoles(departmentIds);
            var clientAdminNavRoles = GetPowerUserNavRoles(departmentIds);
            var openOrgRoles = GetOpenOrgRoles(departments);

            List<string> roles = new List<string>
            {
                RoleNames.AppUser, RoleNames.Anonymous,
                RoleNames.AdminB, RoleNames.PowerUser,
                RoleNames.ClientSpecific,
                adminBRole, paymentRole
            };
            if (isGroupAdmin)
            {
                roles.Add(RoleNames.AdminB);
            }
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

        private List<PraxisClientInfo> GetPraxisUserClientList(List<PraxisClient> departments, PraxisClient primaryDepartment)
        {
            var organization = GetOrganization(primaryDepartment.ParentOrganizationId);
            var organizationName = organization != null ? organization.ClientName : "";

            List<PraxisClientInfo> clientList = new List<PraxisClientInfo>();
            departments.ForEach((department) =>
            {
                var clientInfo = new PraxisClientInfo
                {
                    ClientId = department.ItemId,
                    ClientName = department.ClientName,
                    IsPrimaryDepartment = department.ItemId == primaryDepartment.ItemId,
                    ParentOrganizationId = primaryDepartment.ParentOrganizationId,
                    ParentOrganizationName = organizationName,
                    IsCreateProcessGuideEnabled = department.Navigations.Any(nav => nav.Name == "PROCESS_GUIDE"),
                    Roles = new[] { RoleNames.PowerUser }
                };
                clientList.Add(clientInfo);
            }
            );

            return clientList;
        }

        private List<PraxisClient> GetDepartments(string orgId)
        {
            return _repository.GetItems<PraxisClient>(p => p.ParentOrganizationId == orgId).ToList();
        }

        private PraxisOrganization GetOrganization(string id)
        {
            return _repository.GetItem<PraxisOrganization>(p => p.ItemId == id);
        }
    }
}
