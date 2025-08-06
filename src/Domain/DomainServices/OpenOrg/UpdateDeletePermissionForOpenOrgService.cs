using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.OpenOrg
{
    public class UpdateDeletePermissionForOpenOrgService : IUpdateDeletePermissionForOpenOrg
    {
        private readonly ILogger<UpdateDeletePermissionForOpenOrgService> _logger;
        private readonly IRepository _repository;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly IUpdatePowerUserRole _updatePowerUserRoleService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISaveDataToFeatureRoleMap _saveDataToFeatureRoleMapService;
        private readonly IMongoSecurityService _ecapSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;

        public UpdateDeletePermissionForOpenOrgService(
            ILogger<UpdateDeletePermissionForOpenOrgService> logger,
            IRepository repository,
            IMongoClientRepository mongoClientRepository,
            IUpdatePowerUserRole updatePowerUserRoleService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISaveDataToFeatureRoleMap saveDataToFeatureRoleMapService,
            IMongoSecurityService ecapSecurityService,
            ISecurityContextProvider securityContextProvider,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService)
        {
            _logger = logger;
            _repository = repository;
            _mongoClientRepository = mongoClientRepository;
            _updatePowerUserRoleService = updatePowerUserRoleService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _saveDataToFeatureRoleMapService = saveDataToFeatureRoleMapService;
            _ecapSecurityService = ecapSecurityService;
            _securityContextProvider = securityContextProvider;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
        }

        public async Task<(bool, List<PraxisUser>)> UpdatePermission(string clientId, bool IsOpenOrganization)
        {
            _logger.LogInformation("Going to update delete permission for Open Org./Audit Safe for clientId: {ClientId}.", clientId);
            var updateRole = string.Empty;
            var removeRole = string.Empty;
            try
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("ClientList.ClientId", clientId) & builder.In("ClientList.Roles", new string[] { "poweruser" });
                var collection = _mongoClientRepository.GetCollection(EntityName.PraxisUser);
                var jsonDataList = collection.FindAsync(filter).Result.ToList();
                var powerUserList = BsonSerializer.Deserialize<List<PraxisUser>>(jsonDataList.ToJson());

                if (!IsOpenOrganization)
                {
                    updateRole = $"{RoleNames.Open_Organization}_{clientId}";
                    removeRole = $"{RoleNames.Audit_Safe}_{clientId}";
                }
                else
                {
                    updateRole = $"{RoleNames.Audit_Safe}_{clientId}";
                    removeRole = $"{RoleNames.Open_Organization}_{clientId}";
                }

                var success = PrepareRole(updateRole, clientId);
                if (success)
                {
                    success = await _updatePowerUserRoleService.UpdateRole(powerUserList, updateRole, removeRole);
                    if (success)
                    {
                        success = await UpdatePersonaRoleMapData(clientId, updateRole, removeRole);
                        if (success)
                        {
                            var deleteFeatureRole = updateRole.Split('_');

                            await _repository.DeleteAsync<FeatureRoleMap>(f => f.RoleName == removeRole);
                            _logger.LogInformation("Data has been successfully deleted from {EntityName} entity with RoleName: {RoleName}.", nameof(FeatureRoleMap), removeRole);

                            if (deleteFeatureRole[0] == RoleNames.Open_Organization)
                            {
                                var featureList = _repository.GetItems<PraxisDeleteFeature>(d => true).ToList();
                                _saveDataToFeatureRoleMapService.ProcessData(featureList, updateRole);
                            }
                        }
                    }
                }

                return (true, powerUserList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during update all power user role for Audit safe/Open Org. for clientId:{ClientId}. Exception Message: {Message}. Exception Details: {StackTrace}.", clientId, ex.Message, ex.StackTrace);
                return (false, new List<PraxisUser>());
            }
        }

        private async Task<bool> UpdatePersonaRoleMapData(string clientId, string updateRole, string removeRole)
        {
            _logger.LogInformation("Going to update {EntityName} entity data clientId: {ClientId}.", nameof(PersonaRoleMap), clientId);

            try
            {
                var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PersonaRoleMap>(string.Format("{0}s", "PersonaRoleMap"));
                var filter = Builders<PersonaRoleMap>.Filter.Regex(x => x.Persona, new BsonRegularExpression(clientId, "i")) & Builders<PersonaRoleMap>.Filter.In("PersonaRoles.RoleName", new string[] { "poweruser" });
                var personaRoleList = collection.Find(filter).ToList();
                foreach (var personaRoleMap in personaRoleList)
                {
                    var personaRoles = personaRoleMap.PersonaRoles.Where(r => r.RoleName != removeRole).ToList();
                    personaRoles.Add(new PersonaRole { RoleName = updateRole, IsOptional = false });

                    personaRoleMap.PersonaRoles = personaRoles.ToArray();


                    await _repository.UpdateAsync<PersonaRoleMap>(p => p.ItemId == personaRoleMap.ItemId, personaRoleMap);
                    _logger.LogInformation("Persona Roles has been successfully updated to {EntityName} entity with ItemId: {ItemId}.", nameof(PersonaRoleMap), personaRoleMap.ItemId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during update PersonaRoles property in {EntityName} entity for clientId:{ClientId}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PersonaRoleMap), clientId, ex.Message, ex.StackTrace);
                return false;
            }
        }

        private bool PrepareRole(string role, string clientId)
        {
            _logger.LogInformation("Going to prepare role for Audit safe/Open Org. with role:{Role}.", role);
            try
            {
                var isRoleExists = _ecapSecurityService.IsRoleExist(role);
                var openOrgRole = !isRoleExists ? PrepareRoleToRolesTable(role, clientId) : role;
                var isExist = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == openOrgRole).Result;
                if (!isExist)
                {
                    var parents = _roleHierarchyForPersonaRoleService.GetParentList(RoleNames.PowerUser);
                    var newRoleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Parents = parents.ToList(),
                        Role = openOrgRole
                    };
                    _repository.Save(newRoleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with ItemId: {ItemId}.", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during prepare role for Audit safe/Open Org. with role:{Role}. Exception Message: {Message}. Exception Details: {StackTrace}.", role, ex.Message, ex.StackTrace);
                return false;
            }
        }

        private string PrepareRoleToRolesTable(string roleName, string organizationId)
        {
            _logger.LogInformation("Going to save role: {RoleName} in {EntityName} entity for Client: {OrganizationId}.", roleName, nameof(Role), organizationId);

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
                    RoleName = roleName
                };

                _repository.Save(newRole);
                _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} with ItemId: {ItemId}.", nameof(Role), roleName, newRole.ItemId);
                return roleName;
            }

            return roleName;
        }
    }
}
