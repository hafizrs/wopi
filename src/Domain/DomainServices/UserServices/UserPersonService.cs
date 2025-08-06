using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class UserPersonService : IUserPersonService
    {
        private readonly ILogger<UserPersonService> logger;
        private readonly IRepository repository;
        private readonly IMongoDataService ecapDataService;
        private readonly ILogger<UserPersonService> ecapLogger;
        private readonly IMongoSecurityService ecapSecurityService;
        private readonly IMongoClientRepository mongoClientRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly ISaveDataToFeatureRoleService _saveDataToFeatureRoleService;

        public UserPersonService(ILogger<UserPersonService> ecapLogger, IMongoSecurityService ecapSecurityService,
            IRepository repository, IMongoDataService ecapDataService, IMongoClientRepository mongoClientRepository,
            ILogger<UserPersonService> logger,
            ISecurityContextProvider securityContextProvider,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService,
            IEmailDataBuilder emailDataBuilder,
            ISaveDataToFeatureRoleService saveDataToFeatureRoleService,
            IEmailNotifierService emailNotifierService)
        {
            this.logger = logger;
            _securityContextProvider = securityContextProvider;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
            this.ecapLogger = ecapLogger;
            this.ecapSecurityService = ecapSecurityService;
            this.repository = repository;
            this.ecapDataService = ecapDataService;
            this.mongoClientRepository = mongoClientRepository;
            this.emailDataBuilder = emailDataBuilder;
            this.emailNotifierService = emailNotifierService;
            _saveDataToFeatureRoleService = saveDataToFeatureRoleService;
        }

        // Role Assign To User 
        public void RoleAssignToUser(Person person, IEnumerable<PraxisClientInfo> clientList, bool isTechnicalClient = false)
        {
            var roles = new List<string>
            {
                isTechnicalClient ? RoleNames.TechnicalClient : RoleNames.ClientSpecific
            };

            var user = repository.GetItem<User>(u => u.ItemId.Equals(person.CreatedBy) && !u.IsMarkedToDelete);

            if (user != null)
            {
                foreach (var client in clientList)
                {
                    var roleList = new List<string>();

                    int isPowerUser = Array.IndexOf(client.Roles.ToArray(), RoleNames.PowerUser);
                    int isLeitung = Array.IndexOf(client.Roles.ToArray(), RoleNames.Leitung);
                    int isMpaGroup1 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup1);
                    int isMpaGroup2 = Array.IndexOf(client.Roles.ToArray(), RoleNames.MpaGroup2);

                    var adminRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, client.ClientId);
                    var readRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, client.ClientId);
                    var managerRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, client.ClientId);
                    var eeGroup1RoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup1, client.ClientId);
                    var eeGroup2RoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup2, client.ClientId);

                    roleList.Add(eeGroup1RoleName);
                    roleList.Add(eeGroup2RoleName);
                    PrepareRole(roleList, client.ClientId);

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

                ecapSecurityService.AssignRoleToUser(Guid.Parse(person.CreatedBy), roles, false);
                UpdateConnectionEntityForPerson(person, roles);
            }
            else
            {
                ecapLogger.LogInformation("RoleAssignToUser Get NULL for UserId {UserId}", person.CreatedBy);
                ecapSecurityService.AssignRoleToUser(Guid.Parse(person.CreatedBy), roles);
            }
        }

        private void PrepareRole(List<string> roles, string clientId)
        {
            logger.LogInformation($"Going to prepare dynamic role with role:{string.Join(',', roles)}.");
            foreach (var role in roles)
            {
                try
                {
                    var isRoleExists = ecapSecurityService.IsRoleExist(role);
                    var openOrgRole = !isRoleExists ? PrepareRoleToRolesTable(role, clientId) : role;
                    var isExist = repository.ExistsAsync<RoleHierarchy>(h => h.Role == openOrgRole).Result;
                    if (!isExist)
                    {
                        var parents = _roleHierarchyForPersonaRoleService.GetParentList(RoleNames.MpaGroup1);
                        var newRoleHierarchy = new RoleHierarchy
                        {
                            ItemId = Guid.NewGuid().ToString(),
                            Parents = parents.ToList(),
                            Role = openOrgRole
                        };
                        repository.Save(newRoleHierarchy);
                        logger.LogInformation("Data has been successfully inserted to {EntityName} entity with ItemId: {ItemId}.", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Exception occurred during prepare dynamic role with role: {Role}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", role, ex.Message, ex.StackTrace);
                }
            }
        }

        private string PrepareRoleToRolesTable(string roleName, string organizationId)
        {
            logger.LogInformation("Going to save role: {RoleName} in {EntityName} entity for Client: {OrganizationId}.", roleName, nameof(Role), organizationId);

            var securityContext = _securityContextProvider.GetSecurityContext();
            var rolesAllowTo = new string[] { "appuser" };
            var roleExist = repository.ExistsAsync<Role>(r => r.RoleName == roleName).Result;
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

                repository.Save(newRole);
                logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} and ItemId: {ItemId}.", nameof(Role), roleName, newRole.ItemId);
                return roleName;
            }

            return roleName;
        }

        public void RoleReassignToUser(Person person, IEnumerable<PraxisClientInfo> clientList, bool isTechnicalClient = false)
        {
            var unassignRoles = new List<string>();
            var roles = new List<string>
            {
                isTechnicalClient ? RoleNames.TechnicalClient : RoleNames.ClientSpecific
            };

            var user = repository.GetItem<User>(u => u.ItemId.Equals(person.CreatedBy) && !u.IsMarkedToDelete);

            if (user != null)
            {
                foreach (var client in clientList)
                {
                    var roleList = new List<string>();
                    var adminRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, client.ClientId);
                    var readRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, client.ClientId);
                    var managerRoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, client.ClientId);
                    var eeGroup1RoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup1, client.ClientId);
                    var eeGroup2RoleName = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisEEGroup2, client.ClientId);

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
                    if (isMpaGroup1 > -1)
                    {
                        roles.Add(eeGroup1RoleName);
                    }
                    else
                    {
                        unassignRoles.Add(eeGroup1RoleName);
                    }

                    if (isMpaGroup2 > -1)
                    {
                        roles.Add(eeGroup2RoleName);
                    }
                    else
                    {
                        unassignRoles.Add(eeGroup2RoleName);
                    }
                }
                ecapSecurityService.UnassignRoleFromUser(Guid.Parse(person.CreatedBy), unassignRoles, false);
                ecapSecurityService.AssignRoleToUser(Guid.Parse(person.CreatedBy), roles, false);
                logger.LogInformation("Roles property has been successfully updated for user: {UserId} and roles: {Roles}", user.ItemId, JsonConvert.SerializeObject(roles));

                UpdateConnectionEntityForPerson(person, roles);
            }
            else
            {
                ecapLogger.LogInformation("RoleReassignToUser Get NULL for UserId {UserId}", person.CreatedBy);
                ecapSecurityService.AssignRoleToUser(Guid.Parse(person.CreatedBy), roles);
                logger.LogInformation("Roles property has been successfully updated for user: {UserId} and roles: {Roles}", user?.ItemId, JsonConvert.SerializeObject(roles));
            }
        }

        private void UpdateConnectionEntityForPerson(Person person, List<string> roleList)
        {
            var connections = repository.GetItems<Connection>(c =>
                c.ChildEntityID.Equals(person.ItemId) &&
                c.Tags.Contains(TagName.PersonForUser)
            ).Take(50).Skip(0).ToList();

            foreach (var connection in connections)
            {
                var connectionPermission = new EntityReadWritePermission
                {
                    Id = Guid.Parse(connection.ItemId)
                };
                connectionPermission.RolesAllowedToRead.Add(RoleNames.Admin);
                connectionPermission.RolesAllowedToUpdate.Add(RoleNames.Admin);
                connectionPermission.RolesAllowedToReadForRemove.Add(RoleNames.AppUser);

                if (roleList != null)
                {
                    foreach (var role in roleList)
                    {
                        connectionPermission.RolesAllowedToRead.Add(role);
                        connectionPermission.RolesAllowedToUpdate.Add(role);
                    }
                }

                ecapSecurityService.UpdateEntityReadWritePermission<Connection>(connectionPermission);
            }
        }

        public Person GetByUserId(string userId)
        {
            var filter = Builders<Person>.Filter.In("Tags", new string[]
            {
                TagName.Person
            });
            filter &= Builders<Person>.Filter.Eq("CreatedBy", userId);
            filter &= Builders<Person>.Filter.Eq("IsMarkedToDelete", false);

            var response = GetListByFilter(filter, null, 0, 1);

            return response?.Results?.ElementAt(0);
        }

        public Person GetById(string personId)
        {
            var details = ecapDataService.GetEntityDetials<Person>(
                personId,
                new List<string>
                {
                    TagName.Person
                });

            return details as Person;
        }

        public PersonQueryResponse GetListByIds(List<string> personIds, string searchText = null, int pageNumber = 0, bool useImpersonation = false)
        {
            var builders = Builders<Person>.Filter;
            var filter = builders.In("Tags", new[]
            {
                TagName.Person
            });
            filter &= Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
            filter &= builders.In("_id", personIds.ToArray());
            if (!string.IsNullOrEmpty(searchText))
            {
                filter &= (
                    builders.Regex("DisplayName", $"/{searchText}/i") |
                    builders.Regex("FirstName", $"/{searchText}/i") |
                    builders.Regex("LastName", $"/{searchText}/i") |
                    builders.Regex("Email", $"/{searchText}/i") |
                    builders.Regex("Designation", $"/{searchText}/i") |
                    builders.Regex("PhoneNumber", $"/{searchText}/i")
                );
            }

            return GetListByFilter(filter, null, pageNumber, 100, "CreateDate", true, useImpersonation);
        }

        public PersonQueryResponse GetListByUserIds(List<string> userIds, string searchText = null, int pageNumber = 0, bool useImpersonation = false)
        {
            var builders = Builders<Person>.Filter;
            var filter = builders.In("Tags", new[]
            {
                TagName.Person
            });
            filter &= builders.In("CreatedBy", userIds.ToArray());
            filter &= Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
            if (!string.IsNullOrEmpty(searchText))
            {
                filter &= (
                    builders.Regex("DisplayName", $"/{searchText}/i") |
                    builders.Regex("FirstName", $"/{searchText}/i") |
                    builders.Regex("LastName", $"/{searchText}/i") |
                    builders.Regex("Email", $"/{searchText}/i") |
                    builders.Regex("Designation", $"/{searchText}/i") |
                    builders.Regex("PhoneNumber", $"/{searchText}/i")
                );
            }

            return GetListByFilter(filter, null, pageNumber, 100, "CreateDate", true, useImpersonation);
        }

        public PersonQueryResponse GetAdminList(int pageNumber = 0, bool useImpersonation = false)
        {
            var filter = Builders<Person>.Filter.In("Tags", new[] { TagName.Person });
            filter &= Builders<Person>.Filter.In("Roles", new[]
            {
                RoleNames.Admin
            });
            filter &= Builders<Person>.Filter.Nin("Roles", new[]
            {
                RoleNames.SystemAdmin
            });
            filter &= Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
            return GetListByFilter(filter, null, pageNumber, 100, "CreateDate", true, useImpersonation);
        }

        public PersonQueryResponse GetList(int pageNumber = 0, bool useImpersonation = false)
        {
            var filter = Builders<Person>.Filter.In("Tags", new[] { TagName.Person }) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);

            return GetListByFilter(filter, null, pageNumber, 100, "CreateDate", true, useImpersonation);
        }

        public PersonQueryResponse GetListByClientId(string clientId, int pageNumber = 0, bool useImpersonation = false)
        {
            var filter = Builders<Person>.Filter.In("Tags", new[] { TagName.Person }) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
            filter &= Builders<Person>.Filter.Eq("OrganizationId", clientId);

            return GetListByFilter(filter, null, pageNumber, 100, "CreateDate", true, useImpersonation);
        }

        public PersonQueryResponse GetListByFilter(FilterDefinition<Person> filter, List<string> fieldsToReturn = null,
            int pageNumber = 0, int itemsPerPage = 100, string orderBy = "CreateDate", bool descending = true, bool useImpersonation = true)
        {
            try
            {
                var dataset = ecapDataService.GetListByFilter<Person>(
                    filter, pageNumber, itemsPerPage, null, orderBy, descending, useImpersonation
                );

                if (dataset != null)
                {
                    return new PersonQueryResponse
                    {
                        Results = dataset.Results,
                        StatusCode = dataset.StatusCode,
                        TotalRecordCount = dataset.TotalRecordCount,
                        ErrorMessage = dataset.ErrorMessages?.Count > 0 ? dataset.ErrorMessages[0] : string.Empty
                    };
                }
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation(ex, "GetPersonListByFilter");
            }

            return new PersonQueryResponse();
        }

        public List<Person> GetListByOrgId(string orgId)
        {
            var pageNumber = 0;
            var personList = new List<Person>();

            while (true)
            {
                // Filter Query
                var filter = Builders<Person>.Filter.In("Tags", new string[]
                {
                    TagName.Person
                });
                filter &= Builders<Person>.Filter.Eq("OrganizationId", orgId);
                filter &= Builders<Person>.Filter.Eq("IsMarkedToDelete", false);

                var response = GetListByFilter(filter, null, pageNumber);

                if (response?.Results == null || response.StatusCode != 0 || response.TotalRecordCount == 0 || pageNumber > 100)
                {
                    break;
                }

                personList.AddRange(response.Results);

                if (response.TotalRecordCount > personList.Count)
                {
                    pageNumber += 1;
                }
                else
                {
                    break;
                }
            }

            return personList;
        }

        public List<Person> GetListByRoles(List<string> Roles)
        {
            var persons = mongoClientRepository.GetCollection<Person>();
            var builder = Builders<Person>.Filter;

            var filter = builder.In("Tags", new[]
            {
                TagName.Person
            });

            filter &= builder.Nin("Roles", new[]
            {
                "admin"
            });

            filter &= builder.In("Roles", Roles);
            filter &= builder.Eq("IsMarkedToDelete", false);

            var personList = persons.Find(filter).ToList();

            return personList;
        }

        public void AddRowLevelSecurity(string itemId, string[] clientIds)
        {
            var rolesAllowedToRead = new List<string>() { RoleNames.Admin, RoleNames.TaskController };
            var rolesAllowedToUpdate = new List<string>() { RoleNames.Admin, RoleNames.TaskController };

            foreach (var clientId in clientIds)
            {
                var clientAdminAccessRole = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
                var clientReadAccessRole = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
                var clientManagerAccessRole = ecapSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

                rolesAllowedToRead.Add(clientAdminAccessRole);
                rolesAllowedToRead.Add(clientReadAccessRole);
                rolesAllowedToRead.Add(clientManagerAccessRole);

                // Commented for business requirements
                rolesAllowedToUpdate.Add(clientAdminAccessRole);
                rolesAllowedToUpdate.Add(clientManagerAccessRole);
            }

            var updates = new Dictionary<string, object>
            {
                {"RolesAllowedToRead", rolesAllowedToRead },
                {"RolesAllowedToUpdate", rolesAllowedToUpdate }
            };

            repository.UpdateAsync<Person>(p => p.ItemId == itemId, updates).Wait();
        }
        public void SendEmailToUserForLatestClient(PraxisUser praxisUserInfo)
        {
            if (praxisUserInfo != null && praxisUserInfo.ClientList != null)
            {
                var organizationNames = praxisUserInfo.ClientList.Where(x => x.IsLatest).Select(y => y.ClientName).ToList();
                var emailData = emailDataBuilder.BuildUserUpdateConfirmationEmailData(organizationNames, praxisUserInfo);
                emailNotifierService.SendUserUpdateConfirmationEmail(praxisUserInfo, emailData).GetAwaiter().GetResult();
            }
        }
        public PraxisUserDto MapPraxisUserDto(PraxisUser praxisUser, string itemId = "")
        {
            var clientList = new List<PraxisClientInfoDto>();
            if (praxisUser.ClientList != null && praxisUser.ClientList.Any())
            {
                clientList = praxisUser.ClientList.Select(x => new PraxisClientInfoDto { ClientId = x.ClientId, ClientName = x.ClientName }).ToList();
            }
            else
            {
                clientList.Add(new PraxisClientInfoDto { ClientId = praxisUser.ClientId, ClientName = praxisUser.ClientName });
            }
            return new PraxisUserDto
            {
                ItemId = string.IsNullOrEmpty(itemId) ? Guid.NewGuid().ToString() : itemId,
                Clients = clientList,
                CreateDate = praxisUser.CreateDate,
                CreatedBy = praxisUser.CreatedBy,
                DisplayName = praxisUser.DisplayName,
                ImageId = praxisUser.Image != null && praxisUser.Image.FileId != null ? praxisUser.Image.FileId : null,
                UserId = praxisUser.UserId,
                PraxisUserId = praxisUser.ItemId,
                Tags = praxisUser.Tags,
                TenantId = praxisUser.TenantId,
                IsActive = praxisUser.Active,
                Language = praxisUser.Language,
                IsMarkedToDelete = praxisUser.IsMarkedToDelete,
                IdsAllowedToRead = praxisUser.IdsAllowedToRead,
                IdsAllowedToUpdate = praxisUser.RolesAllowedToUpdate,
                IdsAllowedToDelete = praxisUser.IdsAllowedToDelete,
                IdsAllowedToWrite = praxisUser.IdsAllowedToWrite,
                LastUpdateDate = praxisUser.LastUpdateDate,
                LastUpdatedBy = praxisUser.LastUpdatedBy,
                RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser },
                RolesAllowedToDelete = praxisUser.RolesAllowedToDelete,
                RolesAllowedToUpdate = praxisUser.RolesAllowedToUpdate,
                RolesAllowedToWrite = praxisUser.RolesAllowedToWrite
            };
        }
        public async Task PrepareDataForFeatureRoleMap(string roleName)
        {
            var featureList = new List<NavInfo> {
                new NavInfo {
                    AppType = "business.praxis",
                    AppName = "organization.type.update",
                    FeatureId = "organization.type.update",
                    FeatureName = "organization.type.update"
                },
                new NavInfo
                {
                    AppType = "business.praxis",
                    AppName = "client.payment.update",
                    FeatureId = "client.payment.update",
                    FeatureName = "client.payment.update"
                },
                 new NavInfo
                {
                    AppType = "business.praxis",
                    AppName = "client.addSuppliers",
                    FeatureId = "client.addSuppliers",
                    FeatureName = "client.addSuppliers"
                },
                new NavInfo
                {
                    AppType = "business.praxis",
                    AppName = "client.updateSupplier",
                    FeatureId = "client.updateSupplier",
                    FeatureName = "client.updateSupplier"
                },
                new NavInfo
                {
                    AppType = "business.praxis",
                    AppName = "client.categoryCreate",
                    FeatureId = "client.categoryCreate",
                    FeatureName = "client.categoryCreate"
                }
            };
            await _saveDataToFeatureRoleService.SaveData(featureList, roleName);
        }
    }

}
