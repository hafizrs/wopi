using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using MongoDB.Bson;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.OpenOrg
{
    public class SaveDataToFeatureRoleMap : ISaveDataToFeatureRoleMap
    {
        private readonly ILogger<SaveDataToFeatureRoleMap> _logger;
        private readonly IRepository _repository;

        public SaveDataToFeatureRoleMap(
            ILogger<SaveDataToFeatureRoleMap> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }


        public bool ProcessData(List<PraxisDeleteFeature> deleteFeatureList, string role)
        {
            _logger.LogInformation("Going to save date to {EntityName} entity with feature list {FeatureList} and role: {Role}.", nameof(PraxisDeleteFeature), JsonConvert.SerializeObject(deleteFeatureList), role);
            foreach (var deleteFeature in deleteFeatureList)
            {
                try
                {
                    var isExist = _repository.ExistsAsync<FeatureRoleMap>(f => f.AppName == deleteFeature.AppName && f.RoleName == role).Result;
                    if (!isExist)
                    {
                        var featureRoleMap = new FeatureRoleMap
                        {
                            ItemId = ObjectId.GenerateNewId().ToString(),
                            AppName = deleteFeature.AppName,
                            AppType = deleteFeature.AppType,
                            FeatureId = deleteFeature.FeatureId,
                            FeatureName = deleteFeature.FeatureName,
                            RoleName = role
                        };

                        _repository.Save(featureRoleMap);
                        _logger.LogInformation("Data has been successfully inserted to {EntityName} entity with role name: {RoleName} and app name: {AppName} with ItemId: {ItemId}.", nameof(FeatureRoleMap), role, deleteFeature.AppName, featureRoleMap.ItemId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occured during saving data to {EntityName} entity with role: {Role}. Exception Message: {Message}. Exception details: {StackTrace}.", nameof(FeatureRoleMap), role, ex.Message, ex.StackTrace);
                    return false;
                }
            }
            return true;
        }
    }
}
