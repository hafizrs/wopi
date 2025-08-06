using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionForPowerUser : IRevokePermissionForRoleSpecific
    {
        private readonly IRepository _repository;
        private readonly ILogger<RevokePermissionForPowerUser> _logger;
        private readonly IRevokePermissionForCommonEntities _revokePermissionForCommonEntitiesService;

        public RevokePermissionForPowerUser(
            IRepository repository,
            ILogger<RevokePermissionForPowerUser> logger,
            IRevokePermissionForCommonEntities revokePermissionForCommonEntitiesService)
        {
            _repository = repository;
            _logger = logger;
            _revokePermissionForCommonEntitiesService = revokePermissionForCommonEntitiesService;
        }

        public async Task RevokePermission(string userId, string personId)
        {
            _logger.LogInformation("Going to revoke permission for poweruser role for user: {UserId}", userId);

            try
            {
                await UpdatePermissionForClientData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForCategoryData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForFormData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskConfigData(userId, personId, RoleNames.PowerUser);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForPraxisTaskData(userId, personId, RoleNames.PowerUser);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskScheduleData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskSummaryData(userId);
                await UpdatePermissionForTrainingData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTrainingAnswerData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRiskData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRiskAssessmentData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForEquipmentData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForEquipmentMaintenanceData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRoomData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemConfigData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemData(userId);
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurred during revoke permission from data for poweruser role for user: {UserId}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.", userId, ex.Message, ex.StackTrace);
            }
        }

        private async Task UpdatePermissionForClientData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}.", nameof(PraxisClient), userId);
            try
            {
                var existingClientList = _repository.GetItems<PraxisClient>(c => c.IdsAllowedToUpdate.Contains(userId) && !c.IsMarkedToDelete).ToList();

                foreach (var client in existingClientList)
                {
                    var updatedIds = client.IdsAllowedToUpdate?.Where(i => !i.Contains(userId));

                    client.IdsAllowedToUpdate = updatedIds.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToUpdate", client.IdsAllowedToUpdate},
                    };

                    await _repository.UpdateAsync<PraxisClient>(c => c.ItemId == client.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisClient), client.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update {EntityName} data for user: {UserId}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.", nameof(PraxisClient), userId, ex.Message, ex.StackTrace);
            }
        }

        public async Task UpdatePermissionForTrainingData(string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}.", nameof(PraxisTraining), userId);
            try
            {
                var existingTrainingList = _repository.GetItems<PraxisTraining>(t => t.IdsAllowedToRead.Contains(userId) || t.IdsAllowedToUpdate.Contains(userId) && !t.IsMarkedToDelete).ToList();
                foreach (var training in existingTrainingList)
                {
                    var updatedIdsAllowToRead = training.IdsAllowedToRead?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToUpdate = training.IdsAllowedToUpdate?.Where(r => !r.Contains(userId));
                    var updatedIdsAllowToDelete = training.IdsAllowedToDelete?.Where(r => !r.Contains(userId));

                    training.IdsAllowedToRead = updatedIdsAllowToRead.ToArray();
                    training.IdsAllowedToUpdate = updatedIdsAllowToUpdate.ToArray();
                    training.IdsAllowedToDelete = updatedIdsAllowToDelete.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"IdsAllowedToRead", training.IdsAllowedToRead},
                        {"IdsAllowedToUpdate", training.IdsAllowedToUpdate},
                        {"IdsAllowedToDelete", training.IdsAllowedToDelete}
                    };

                    await _repository.UpdateAsync<PraxisTraining>(a => a.ItemId == training.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisTraining), training.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during update {EntityName} data for user: {UserId}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.", nameof(PraxisTraining), userId, ex.Message, ex.StackTrace);
            }
        }
    }
}
