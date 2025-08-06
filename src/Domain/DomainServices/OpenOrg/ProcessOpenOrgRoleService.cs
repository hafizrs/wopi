using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.OpenOrg
{
    public class ProcessOpenOrgRoleService : IProcessOpenOrgRole
    {
        private readonly ILogger<ProcessOpenOrgRoleService> _logger;
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _ecapSecurityService;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForPersonaRoleService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISaveDataToFeatureRoleMap _saveDataToFeatureRoleMapService;

        public ProcessOpenOrgRoleService(
            ILogger<ProcessOpenOrgRoleService> logger,
            IRepository repository,
            IMongoSecurityService ecapSecurityService,
            IRoleHierarchyForPersonaRoleService roleHierarchyForPersonaRoleService,
            ISecurityContextProvider securityContextProvider,
            ISaveDataToFeatureRoleMap saveDataToFeatureRoleMapService)
        {
            _logger = logger;
            _repository = repository;
            _ecapSecurityService = ecapSecurityService;
            _roleHierarchyForPersonaRoleService = roleHierarchyForPersonaRoleService;
            _securityContextProvider = securityContextProvider;
            _saveDataToFeatureRoleMapService = saveDataToFeatureRoleMapService;
        }
        public OpenOrganizationResponse ProcessRole(string clientId, bool IsOpenOrganization)
        {
            try
            {
                var role = !IsOpenOrganization ? $"{RoleNames.Open_Organization}_{clientId}" : $"{RoleNames.Audit_Safe}_{clientId}";
                var success = PrepareRole(role, clientId);
                if (success)
                {
                    var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId && !c.IsMarkedToDelete);
                    if (client != null && !client.IsOpenOrganization.Value)
                    {
                        var featureList = _repository.GetItems<PraxisDeleteFeature>(d => true).ToList();
                        _saveDataToFeatureRoleMapService.ProcessData(featureList, role);
                    }
                }
                return new OpenOrganizationResponse
                {
                    StatusCode = 200,
                    Message = string.Empty,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during process role for Audit safe/Open Org. for clientId:{ClientId}. Exception Message: {Message}. Exception Details: {StackTrace}.", clientId, ex.Message, ex.StackTrace);
                return new OpenOrganizationResponse
                {
                    StatusCode = 500,
                    Message = "Exception occured during process Audit safe/Open Org roles.",
                    Role = string.Empty
                };
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
