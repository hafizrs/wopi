using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PrepareNewRole : IPrepareNewRole
    {
        private readonly ILogger<PrepareNewRole> _logger;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRoleHierarchyForPersonaRoleService _roleHierarchyForRole;

        public PrepareNewRole(
            ILogger<PrepareNewRole> logger,
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IRoleHierarchyForPersonaRoleService roleHierarchyForRole)
        {
            _logger = logger;
            _mongoSecurityService = mongoSecurityService;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _roleHierarchyForRole = roleHierarchyForRole;
        }

        public string SaveRole(string role, string clientId, string chieldRole, bool isDynamic)
        {
            _logger.LogInformation("Going to prepare dynamic role with role: {Role}", role);
            try
            {
                var isRoleExists = _mongoSecurityService.IsRoleExist(role);
                var newRole = !isRoleExists ? SaveDataToRolesTable(role, clientId, isDynamic) : role;
                var isExist = _repository.ExistsAsync<RoleHierarchy>(h => h.Role == newRole).Result;
                if (!isExist)
                {
                    var parents =
                        _roleHierarchyForRole.GetParentList(chieldRole);
                    var newRoleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Parents = parents.ToList(),
                        Role = newRole
                    };
                    _repository.Save(newRoleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {Entity} entity with ItemId: {ItemId}", nameof(RoleHierarchy), newRoleHierarchy.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during prepare dynamic role with role: {Role}. Exception Message: {Message}. Exception Details: {StackTrace}.", role, ex.Message, ex.StackTrace);
                return string.Empty;
            }

            return role;
        }

        private string SaveDataToRolesTable(string roleName, string organizationId, bool isDynamic)
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
                    IsDynamic = isDynamic
                };

                _repository.Save(newRole);
                _logger.LogInformation("Data has been successfully inserted to {Entity} entity with role name: {RoleName} with ItemId: {ItemId}.", nameof(Role), roleName, newRole.ItemId);
                return roleName;
            }

            return roleName;
        }
    }
}
