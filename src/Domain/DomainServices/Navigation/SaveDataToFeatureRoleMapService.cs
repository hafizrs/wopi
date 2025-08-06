using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class SaveDataToFeatureRoleMapService : ISaveDataToFeatureRoleService
    {
        private readonly IRepository _repository;
        private readonly ILogger<SaveDataToFeatureRoleMapService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;

        public SaveDataToFeatureRoleMapService(
            IRepository repository,
            ILogger<SaveDataToFeatureRoleMapService> logger,
            ISecurityContextProvider securityContextProvider)
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> SaveData(List<NavInfo> navigationDataList, string navRole)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            if (navigationDataList.Count == 0) return true;
            _logger.LogInformation(
                $"Going to save data to {nameof(FeatureRoleMap)} entity with role: {navRole} " +
                $"and navigationList: \n{JsonConvert.SerializeObject(navigationDataList)}  " +
                $"TenantId: {securityContext.TenantId}."
            );
            try
            {
                foreach (var navData in navigationDataList)
                {
                    if (
                        await _repository.ExistsAsync<FeatureRoleMap>(
                            f => f.AppName == navData.AppName && f.RoleName == navRole
                        )
                    ) continue;
                    
                    var featureRoleMap = new FeatureRoleMap
                    {
                        ItemId = ObjectId.GenerateNewId().ToString(),
                        AppName = navData.AppName,
                        AppType = navData.AppType,
                        FeatureId = navData.FeatureId,
                        FeatureName = navData.FeatureName,
                        RoleName = navRole
                    };

                    Console.WriteLine(navData.FeatureId, navRole);
                        
                    await _repository.SaveAsync(featureRoleMap);
                    _logger.LogInformation(
                        $"Data has been successfully inserted to {nameof(FeatureRoleMap)} entity with role " +
                        $"name: {navRole} and app name: {navData.AppName} with ItemId: {featureRoleMap.ItemId}."
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured during saving data to {EntityName} entity with role: {NavRole}. TenantId: {TenantId}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    nameof(FeatureRoleMap), navRole, securityContext.TenantId, ex.Message, ex.StackTrace
                );
                return false;
            }
        }
    }
}
