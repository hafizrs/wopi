using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class ProcessNewUserDataService : IProcessUserInformation
    {
        private readonly ILogger<ProcessNewUserDataService> _logger;
        private readonly IRepository _repository;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversion;
        private readonly IProcessUserDataByUam _processUserDataByUamService;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IUpdateDeletePermissionForOpenOrg _updateDeletePermissionForOpenOrg;
        private readonly IPrepareNewRole _prepareNewRoleService;
        private readonly IUserPersonService _userPersonService;
        private readonly DeleteDataForUser _deleteDataForUser;
        private readonly IPraxisOrganizationService _praxisOrganizationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;

        public ProcessNewUserDataService(
            ILogger<ProcessNewUserDataService> logger,
            IRepository repository,
            IProcessUserDataByUam processUserDataByUamService,
            IMongoSecurityService mongoSecurityService,
            ISecurityContextProvider securityContextProvider,
            IDocumentUploadAndConversion documentUploadAndConversion,
            IUpdateDeletePermissionForOpenOrg updateDeletePermissionForOpenOrg,
            IPrepareNewRole prepareNewRoleService,
            IUserPersonService userPersonService,
            DeleteDataForUser deleteDataForUser,
            IPraxisOrganizationService praxisOrganizationService,
            IUserRoleService userRoleService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService)
        {
            _logger = logger;
            _repository = repository;
            _processUserDataByUamService = processUserDataByUamService;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _documentUploadAndConversion = documentUploadAndConversion;
            _updateDeletePermissionForOpenOrg = updateDeletePermissionForOpenOrg;
            _prepareNewRoleService = prepareNewRoleService;
            _userPersonService = userPersonService;
            _deleteDataForUser = deleteDataForUser;
            _praxisOrganizationService = praxisOrganizationService;
            _userRoleService = userRoleService;
            _riqsAdminsCreateUpdateService = riqsAdminsCreateUpdateService;
        }

        public Task<bool> ProcessData(
            PraxisUser praxisUserInformation,
            FileInformation fileInformation)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ProcessData(
            PersonInfo userInformation,
            PraxisClient primaryDepartment,
            string designation)
        {
            try
            {
                _logger.LogInformation("Going to process new user data with email: {Email}.", userInformation.PersonalInformation.Email);

                var departments = GetDepartments(primaryDepartment.ParentOrganizationId);
                var departmentIds = departments.Select(x => x.ItemId).ToList();

                var personInformation = PrepareDataForUamService(
                    userInformation.PersonalInformation,
                    primaryDepartment,
                    departments,
                    departmentIds,
                    userInformation.IsGroupAdmin);
                var response = await _processUserDataByUamService.SaveData(personInformation);

                if (response.Item1)
                {
                    if (!string.IsNullOrEmpty(userInformation.FileName) &&
                        !string.IsNullOrEmpty(userInformation.FileId))
                    {
                        var fileId = userInformation.FileId;

                        var success = await SaveDataToPraxisUserTable(
                            userInformation.PersonalInformation,
                            primaryDepartment,
                            departments,
                            departmentIds,
                            response.userId,
                            designation,
                            userInformation.IsGroupAdmin,
                            fileId,
                            userInformation.FileName
                        );

                        if (success)
                        {
                            await _updateDeletePermissionForOpenOrg.UpdatePermission(primaryDepartment.ItemId, false);
                            await _documentUploadAndConversion.FileConversion(fileId, TagName.ProfileImageOfPerson);
                        }
                    }
                    else
                    {
                        var success = await SaveDataToPraxisUserTable(
                            userInformation.PersonalInformation,
                            primaryDepartment,
                            departments,
                            departmentIds,
                            response.userId,
                            designation,
                            userInformation.IsGroupAdmin
                        );
                        if (success)
                        {
                            await _updateDeletePermissionForOpenOrg.UpdatePermission(primaryDepartment.ItemId, false);
                        }
                    }

                    await UpdateOrganization(personInformation.Email, personInformation.Roles.ToList());


                    await _userPersonService.PrepareDataForFeatureRoleMap(
                        $"{RoleNames.PowerUser_Dynamic}_{primaryDepartment.ItemId}"
                    );
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during processing new user data with email: {Email}. Exception Message: {Message}. Exception Details: {StackTrace}.", userInformation.PersonalInformation.Email, ex.Message, ex.StackTrace);

                _deleteDataForUser.DeleteData("", userInformation.PersonalInformation.UserId);
                return false;
            }
        }

        private async Task UpdateOrganization(string email, List<string> roles)
        {
            var adminBDynamicRole = roles.FirstOrDefault(r => r.Contains(RoleNames.AdminB_Dynamic));
            if (!string.IsNullOrEmpty(adminBDynamicRole) && !roles.Contains(RoleNames.GroupAdmin))
            {
                string orgId = adminBDynamicRole.Split('_')[1];
                await _praxisOrganizationService.UpdateOrganizationAdminIds(orgId, email, "Created");
            }
        }

        private async Task<bool> SaveDataToPraxisUserTable(
            PersonInformation personInformation,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            string userId,
            string designation,
            bool isGroupAdmin,
            string fileId = null,
            string fileName = null)
        {
            try
            {
                var existingPerson = _repository.GetItem<Person>(p => p.Email == personInformation.Email);
                if (existingPerson != null)
                {
                    var praxisUser = MapPraxisUserData(
                        personInformation,
                        primaryDepartment,
                        departments,
                        departmentIds,
                        existingPerson.ItemId,
                        userId,
                        isGroupAdmin,
                        fileId,
                        fileName
                    );

                    await _repository.SaveAsync<PraxisUser>(praxisUser);
                    PraxisUserDto praxisUserDtoInfo = _userPersonService.MapPraxisUserDto(praxisUser);
                    await _repository.SaveAsync<PraxisUserDto>(praxisUserDtoInfo);
                    await _praxisOrganizationService.UpdateAdminDeputyAdminId(
                        primaryDepartment.ParentOrganizationId,
                        existingPerson.ItemId,
                        designation
                    );
                    var rolesAllowedToRead = new List<string>();
                    rolesAllowedToRead.AddRange(existingPerson.RolesAllowedToRead);
                    rolesAllowedToRead.AddRange(praxisUser.RolesAllowedToRead);
                    existingPerson.RolesAllowedToRead = rolesAllowedToRead.Distinct().ToArray();
                    await _repository.UpdateAsync(p => p.ItemId.Equals(existingPerson.ItemId), existingPerson);

                    if (isGroupAdmin) await _riqsAdminsCreateUpdateService.CreateUpdateRiqsGroupAdmin(praxisUser);

                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with Item: {ItemId}.", nameof(PraxisUser), praxisUser.ItemId);
                }
                else
                {
                    _logger.LogError("No data found in {EntityName} entity with email: {Email}", nameof(Person), personInformation.Email);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during saving user data to {EntityName} entity. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisUser), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private PraxisUser MapPraxisUserData(
            PersonInformation personInformation,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            string personItemId,
            string userId,
            bool isGroupAdmin,
            string fileId = null,
            string fileName = null)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

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

            List<string> rolesAllowToDelete = new List<string> { RoleNames.Admin };

            var clientList = GetPraxisUserClientList(departments, primaryDepartment);

            var praxisUser = new PraxisUser
            {
                ItemId = personItemId,
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = userId,
                Language = securityContext.Language,
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdatedBy = userId,
                Tags = new[] { "Is-Valid-PraxisUser" },
                TenantId = securityContext.TenantId,
                IsMarkedToDelete = false,
                RolesAllowedToRead = rolesAllowToRead.ToArray(),
                RolesAllowedToUpdate = rolesAllowToUpdate.ToArray(),
                RolesAllowedToDelete = rolesAllowToDelete.ToArray(),
                UserId = userId,
                //ClientId = primaryDepartment.ItemId,
                //ClientName = primaryDepartment.ClientName,
                Salutation = personInformation.Salutation,
                FirstName = personInformation.FirstName,
                LastName = personInformation.LastName,
                DisplayName = personInformation.DisplayName,
                Gender = personInformation.Gender,
                DateOfBirth = personInformation.DateOfBirth,
                Nationality = personInformation.Nationality,
                MotherTongue = personInformation.MotherTongue,
                OtherLanguage = personInformation.OtherLanguage,
                Email = personInformation.Email.ToLower(),
                Image = new PraxisImage(),
                Roles = roles,
                ClientList = clientList,
                Skills = new string[] { },
                Specialities = new PraxisSpeciality[] { },
                ShowIntroductionTutorial = true
            };
            if (fileId != null)
                praxisUser.Image = new PraxisImage()
                {
                    FileId = fileId,
                    FileName = fileName
                };

            return praxisUser;
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

        private PersonInformation PrepareDataForUamService(
            PersonInformation userInformation,
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            bool isGroupAdmin)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var tags = new[] { "Is-Valid-PraxisUser" };
            var roles = GetUserRoles(primaryDepartment, departments, departmentIds, isGroupAdmin).ToArray();

            userInformation.ItemId = Guid.NewGuid().ToString();
            userInformation.UserId = userInformation.ItemId;
            userInformation.Roles = roles;
            userInformation.TwoFactorEnabled = true;
            userInformation.Tags = tags;
            userInformation.MotherTongue = securityContext.Language;
            userInformation.PersonaEnabled = false;
            userInformation.UserName = userInformation.Email.ToLower();
            userInformation.RegisteredBy = 1;
            userInformation.Language = securityContext.Language;
            userInformation.Password = "";
            userInformation.PersonInfo = new Dictionary<string, object>
            {
                { "FirstName", userInformation.FirstName },
                { "LastName", userInformation.LastName },
                { "DisplayName", userInformation.DisplayName },
                { "Email", userInformation.Email.ToLower() },
                { "Roles", roles },
                { "PhoneNumber", userInformation.PhoneNumber },
                { "Tags", tags },
                { "Salutation", userInformation.Salutation },
                { "ProfileImageId", userInformation.ProfileImageId },
                //{ "OrganizationId", primaryDepartment.ItemId },
                //{ "Organization", primaryDepartment.ClientName },
                //{ "OrganizationNames", new[] { primaryDepartment.ClientName } }
            };

            return userInformation;
        }

        private List<string> GetUserRoles(
            PraxisClient primaryDepartment,
            List<PraxisClient> departments,
            List<string> departmentIds,
            bool isGroupAdmin)
        {
            var adminBRole = _userRoleService.CreateRole(
                $"{RoleNames.AdminB_Dynamic}_{primaryDepartment.ParentOrganizationId}",
                true,
                RoleNames.AdminB);
            var orgReadRole = _userRoleService.CreateRole(
                $"{RoleNames.Organization_Read_Dynamic}_{primaryDepartment.ParentOrganizationId}",
                true,
                RoleNames.Organization_Read_Dynamic);
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
                adminBRole, orgReadRole, paymentRole
            };
            if (isGroupAdmin)
            {
                roles.Add(RoleNames.GroupAdmin);
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