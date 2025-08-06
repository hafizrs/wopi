using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class UpdateDynamicNavigationService : IDynamicNavigationPreparation
    {
        private readonly ILogger<UpdateDynamicNavigationService> _logger;
        private readonly IRepository _repository;
        private readonly ISaveDataToFeatureRoleService _saveDataToFeatureRoleMapService;
        private readonly ISecurityContextProvider _securityContextProvider;


        public UpdateDynamicNavigationService(
            ILogger<UpdateDynamicNavigationService> logger,
            IRepository repository,
            ISaveDataToFeatureRoleService saveDataToFeatureRoleMapService,
            ISecurityContextProvider securityContextProvider
        )
        {
            _logger = logger;
            _repository = repository;
            _saveDataToFeatureRoleMapService = saveDataToFeatureRoleMapService;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> ProcessNavigationData(string organizationId, List<NavInfo> navigationList)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            _logger.LogInformation(
                $"Going to update data to {nameof(FeatureRoleMap)} entity for " +
                $"OrganizationId: {organizationId}. TenantId: {securityContext.TenantId}."
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
                var existingFeatureRoleMapList =
                    _repository.GetItems<FeatureRoleMap>(f => navRoleList.Contains(f.RoleName)).ToList();
                if (existingFeatureRoleMapList.Count > 0)
                {
                    var roleWiseList = existingFeatureRoleMapList
                        .Where(f => navRoleList.Contains(f.RoleName))
                        .GroupBy(g => g.RoleName)
                        .ToList();

                    var checkRoleMapDatas = roleWiseList.Select(
                        r => CheckNavigationListByRoleWise(navigationList, r.ToList(), r.Key)
                    );
                    return (await Task.WhenAll(checkRoleMapDatas)).All(r => r);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured during updating data to {EntityName} entity for OrganizationId: {OrganizationId}. TenantId: {TenantId}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    nameof(FeatureRoleMap), organizationId, securityContext.TenantId, ex.Message, ex.StackTrace
                );
                return false;
            }
        }

        private async Task<bool> CheckNavigationListByRoleWise(
            List<NavInfo> navigationList,
            List<FeatureRoleMap> exisitngRoleMapList,
            string role
        )
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var roleName = role.Split('_')[0];
            try
            {
                if (roleName.Equals(RoleNames.PowerUser_Nav) || roleName.Equals(RoleNames.Leitung_Nav))
                {
                    if (exisitngRoleMapList.Count > navigationList.Count)
                    {
                        await DeleteFeatureRoleMapData(exisitngRoleMapList, navigationList);
                    }
                    else if (navigationList.Count > exisitngRoleMapList.Count)
                    {
                        await InsertNewNavDataToFeatureRoleMap(exisitngRoleMapList, navigationList, role);
                    }
                    else if (navigationList.Count == exisitngRoleMapList.Count)
                    {
                        var existingAppNameList = exisitngRoleMapList.Select(s => s.AppName).ToList();
                        var updateAppList = navigationList.Select(s => s.AppName).ToList();
                        var totalChange = existingAppNameList.Except(updateAppList).ToList();
                        if (totalChange.Count > 0)
                        {
                            await DeleteFeatureRoleMapData(exisitngRoleMapList, navigationList);
                            await InsertNewNavDataToFeatureRoleMap(exisitngRoleMapList, navigationList, role);
                        }
                    }
                }
                else
                {
                    var removeNavList = roleName switch
                    {
                        RoleNames.MpaGroup1_Nav => NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup1.ToList(),
                        RoleNames.MpaGroup2_Nav => NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup2.ToList(),
                        _ => new List<string>()
                    };

                    var userNavList = navigationList.Where(n => !removeNavList.Contains(n.AppName)).ToList();

                    if (exisitngRoleMapList.Count > userNavList.Count)
                    {
                        await DeleteFeatureRoleMapData(exisitngRoleMapList, userNavList);
                    }
                    else if (userNavList.Count > exisitngRoleMapList.Count)
                    {
                        await InsertNewNavDataToFeatureRoleMap(exisitngRoleMapList, userNavList, role);
                    }
                    else if (userNavList.Count == exisitngRoleMapList.Count)
                    {
                        var existingAppNameList = exisitngRoleMapList.Select(s => s.AppName).ToList();
                        var updateAppList = userNavList.Select(s => s.AppName).ToList();
                        var totalChange = existingAppNameList.Except(updateAppList).ToList();
                        if (totalChange.Count > 0)
                        {
                            await DeleteFeatureRoleMapData(exisitngRoleMapList, userNavList);
                            await InsertNewNavDataToFeatureRoleMap(exisitngRoleMapList, userNavList, role);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured. TenantId: {TenantId}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    securityContext.TenantId, ex.Message, ex.StackTrace
                );
                return false;
            }
        }

        private async Task DeleteFeatureRoleMapData(
            List<FeatureRoleMap> exisitngRoleMapList,
            List<NavInfo> updateNavList
        )
        {
            var newAppNameList = updateNavList.Select(n => n.AppName).ToList();
            var extraRoleMapList = exisitngRoleMapList.Where(f => !newAppNameList.Contains(f.AppName)).ToList();
            foreach (var featureRoleMap in extraRoleMapList)
            {
                await _repository.DeleteAsync<FeatureRoleMap>(f => f.ItemId == featureRoleMap.ItemId);
            }
        }

        private async Task InsertNewNavDataToFeatureRoleMap(
            List<FeatureRoleMap> existingRoleMapList,
            List<NavInfo> updateNavList,
            string role
        )
        {
            var existingAppNameList = existingRoleMapList.Select(r => r.AppName).ToList();
            var newRoleMapList = updateNavList.Where(n => !existingAppNameList.Contains(n.AppName)).ToList();

            await _saveDataToFeatureRoleMapService.SaveData(newRoleMapList, role);
        }
    }
}