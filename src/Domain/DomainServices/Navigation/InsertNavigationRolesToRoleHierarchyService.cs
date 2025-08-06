using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class InsertNavigationRolesToRoleHierarchyService : IInsertNavigationRolesToRoleHierarchy
    {
        private readonly ILogger<InsertNavigationRolesToRoleHierarchyService> _logger;
        private readonly IRepository _repository;

        public InsertNavigationRolesToRoleHierarchyService(
            ILogger<InsertNavigationRolesToRoleHierarchyService> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<bool> InsertRoleHierarchy(string organizationId, string navRole)
        {
            try
            {
                _logger.LogInformation("Going to save all navigation roles to {EntityName} for organizationId: {OrganizationId}.", nameof(RoleHierarchy), organizationId);

                var powerUserParents = new List<string> { "admin", "system_admin", "task_controller", "poweruser" };
                var leitungParents = new List<string> { "admin", "system_admin", "task_controller", "poweruser" };
                var mpaGroupOneParents = new List<string> { "admin", "system_admin", "task_controller", "poweruser", "leitung" };
                var mpaGroupOTwoParents = new List<string> { "admin", "system_admin", "task_controller", "poweruser", "leitung" };

                if (await _repository.ExistsAsync<RoleHierarchy>(r => r.Role == navRole)) return true;
                
                var parentList = GetNavigationRole(navRole) switch
                {
                    "poweruser" => powerUserParents,
                    "leitung" => leitungParents,
                    "mpa-group-1" => mpaGroupOneParents,
                    "mpa-group-2" => mpaGroupOTwoParents,
                    _ => null
                };

                if (parentList != null)
                {
                    var roleHierarchy = new RoleHierarchy
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Role = navRole,
                        Parents = parentList
                    };

                    await _repository.SaveAsync(roleHierarchy);
                    _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName}.", nameof(RoleHierarchy), navRole);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during inserting data to {EntityName} entity. Exception Message: {Message}. Exception details: {StackTrace}.", nameof(RoleHierarchy), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private string GetNavigationRole(string role)
        {
            var splitRole = role.Split('_');
            return splitRole[0] switch
            {
                "poweruser-nav" => "poweruser",
                "leitung-nav" => "leitung",
                "mpa-group-1-nav" => "mpa-group-1",
                "mpa-group-2-nav" => "mpa-group-2",
                _ => null
            };
        }
    }
}
