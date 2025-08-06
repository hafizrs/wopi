using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class InsertDynamicNavigationService : IDynamicNavigationPreparation
    {
        private readonly ILogger<InsertDynamicNavigationService> _logger;
        private readonly IPrepareNavigationRoleByOrganization _prepareNavigationRoleByOrganization;
        private readonly ISaveDataToFeatureRoleService _saveDataToFeatureRoleService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IInsertNavigationRolesToRoleHierarchy _roleHierarchyService;

        public InsertDynamicNavigationService(
            ILogger<InsertDynamicNavigationService> logger,
            IPrepareNavigationRoleByOrganization prepareNavigationRoleByOrganization,
            ISaveDataToFeatureRoleService saveDataToFeatureRoleService,
            ISecurityContextProvider securityContextProvider,
            IInsertNavigationRolesToRoleHierarchy roleHierarchyService)
        {
            _logger = logger;
            _prepareNavigationRoleByOrganization = prepareNavigationRoleByOrganization;
            _saveDataToFeatureRoleService = saveDataToFeatureRoleService;
            _securityContextProvider = securityContextProvider;
            _roleHierarchyService = roleHierarchyService;
        }

        public async Task<bool> ProcessNavigationData(string organizationId, List<NavInfo> navigationList)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            _logger.LogInformation(
                $"Going to save data to {nameof(FeatureRoleMap)} entity for OrganizationId: {organizationId}. " +
                $"TenantId: {securityContext.TenantId}."
            );
            
            try
            {
                var navRoleList = new List<string>
                {
                    $"{RoleNames.PowerUser_Nav}_{organizationId}", 
                    $"{RoleNames.Leitung_Nav}_{organizationId}",
                    $"{RoleNames.MpaGroup1_Nav}_{organizationId}", 
                    $"{RoleNames.MpaGroup2_Nav}_{organizationId}"
                };

                var adminManagerNavList = navigationList;
                
                var eeGroupOneNavList = navigationList
                    .Where(n => !NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup1.Contains(n.AppName))
                    .ToList();
                
                var eeGroupTwoNavList = navigationList
                    .Where(n => !NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup2.Contains(n.AppName))
                    .ToList();

                foreach (var role in navRoleList)
                {
                    switch (role.Split('_')[0])
                    {
                        case RoleNames.PowerUser_Nav: 
                        case RoleNames.Leitung_Nav: 
                            await SaveNavigationDataToFeatureRoleMap(organizationId, role, adminManagerNavList);
                            break;
                        case RoleNames.MpaGroup1_Nav: 
                            await SaveNavigationDataToFeatureRoleMap(organizationId, role, eeGroupOneNavList);
                            break;
                        case RoleNames.MpaGroup2_Nav: 
                            await SaveNavigationDataToFeatureRoleMap(organizationId, role, eeGroupTwoNavList);
                            break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured during saving data to {EntityName} entity for OrganizationId: {OrganizationId}. TenantId: {TenantId}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    nameof(FeatureRoleMap), organizationId, securityContext.TenantId, ex.Message, ex.StackTrace
                );
                return false;
            }
        }

        private async Task SaveNavigationDataToFeatureRoleMap(
            string organizationId,
            string roleName,
            List<NavInfo> navigationList
        )
        {
            var hierarchyStatus = await _roleHierarchyService.InsertRoleHierarchy(organizationId, roleName);
            if (hierarchyStatus)
            {
                var rolePreparationStatus = await _prepareNavigationRoleByOrganization.PrepareRole(roleName, organizationId);
                if (rolePreparationStatus)
                {
                    _logger.LogInformation("Going to save FeatureRoleMaps for the following nav access");
                    _logger.LogInformation("RoleName: {RoleName}", roleName);
                    _logger.LogInformation(
                        "Navigation list: {NavigationList}", JsonConvert.SerializeObject(navigationList.Select(nav => nav.FeatureId), Formatting.Indented)
                    );
                    await _saveDataToFeatureRoleService.SaveData(navigationList, roleName);
                }
            }
        }
    }
}
