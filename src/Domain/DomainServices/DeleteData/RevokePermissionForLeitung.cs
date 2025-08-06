using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionForLeitung : IRevokePermissionForRoleSpecific
    {
        private readonly IRepository _repository;
        private readonly ILogger<RevokePermissionForLeitung> _logger;
        private readonly IRevokePermissionForCommonEntities _revokePermissionForCommonEntitiesService;

        public RevokePermissionForLeitung(
            IRepository repository,
            ILogger<RevokePermissionForLeitung> logger,
            IRevokePermissionForCommonEntities revokePermissionForCommonEntitiesService)
        {
            _repository = repository;
            _logger = logger;
            _revokePermissionForCommonEntitiesService = revokePermissionForCommonEntitiesService;
        }
        public async Task RevokePermission(string userId, string personId)
        {
            _logger.LogInformation("Going to revoke permission for leitung role for user: {UserId}", userId);

            try
            {
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForCategoryData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForFormData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskConfigData(userId, personId, RoleNames.Leitung);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForPraxisTaskData(userId, personId, RoleNames.Leitung);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskScheduleData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTaskSummaryData(userId);
                await UpdatePermissionForTrainingData(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTrainingAnswerData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRiskData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRiskAssessmentData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForEquipmentData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForEquipmentMaintenanceData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForRoomData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemConfigData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemData(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during revoke permission from data for leitung role for user: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }

        public async Task UpdatePermissionForTrainingData(string personId, string userId)
        {
            _logger.LogInformation("Going to update all permission for all {EntityName} entity data for user: {UserId}.", nameof(PraxisTraining), userId);
            try
            {
                var existingTrainingList = _repository.GetItems<PraxisTraining>(t => t.SpecificControllingMembers.Contains(personId) && !t.IsMarkedToDelete).ToList();
                foreach (var trining in existingTrainingList)
                {
                    var specificControllingMembers = trining.SpecificControllingMembers?.Where(r => !r.Contains(personId));
                    trining.SpecificControllingMembers = specificControllingMembers.ToArray();

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime()},
                        {"SpecificControllingMembers", trining.SpecificControllingMembers}
                    };

                    await _repository.UpdateAsync<PraxisTraining>(a => a.ItemId == trining.ItemId, updates);
                    _logger.LogInformation("Data has been successfully updated for {EntityName} entity with ItemId: {ItemId}.", nameof(PraxisTraining), trining.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during update {nameof(PraxisTraining)} data for user: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }
    }
}
