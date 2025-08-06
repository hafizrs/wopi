using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class PrepareNavigationRoleByOrganizationService : IPrepareNavigationRoleByOrganization
    {
        private readonly ILogger<PrepareNavigationRoleByOrganizationService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;

        public PrepareNavigationRoleByOrganizationService(
            ILogger<PrepareNavigationRoleByOrganizationService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> PrepareRole(string roleName, string organizationId)
        {
            _logger.LogInformation("Going to check navigation role: {RoleName} exists in {EntityName} entity for Organization: {OrganizationId}.", roleName, nameof(Role), organizationId);

            var securityContext = _securityContextProvider.GetSecurityContext();
            var rolesAllowTo = new[] {"appuser"};
            var roleExist = await _repository.ExistsAsync<Role>(r => r.RoleName == roleName);
            if (!roleExist)
            {
                var newRole=new Role
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow,
                    CreatedBy = securityContext.UserId,
                    Language = "en-US",
                    LastUpdateDate = DateTime.UtcNow,
                    LastUpdatedBy = securityContext.UserId,
                    Tags = new []{ "built-in" },
                    TenantId = securityContext.TenantId,
                    RolesAllowedToRead = rolesAllowTo,
                    RolesAllowedToUpdate = rolesAllowTo,
                    RoleName = roleName
                };

                await _repository.SaveAsync(newRole);
                _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} with ItemId: {ItemId}.", nameof(Role), roleName, newRole.ItemId);
                return true;
            }

            return true;
        }
    }
}
