using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionForEEGroupOne : IRevokePermissionForRoleSpecific
    {
        private readonly ILogger<RevokePermissionForEEGroupOne> _logger;
        private readonly IRevokePermissionForCommonEntities _revokePermissionForCommonEntitiesService;

        public RevokePermissionForEEGroupOne(
            ILogger<RevokePermissionForEEGroupOne> logger,
            IRevokePermissionForCommonEntities revokePermissionForCommonEntitiesService)
        {
            _logger = logger;
            _revokePermissionForCommonEntitiesService = revokePermissionForCommonEntitiesService;
        }
        public async Task RevokePermission(string userId, string personId)
        {
            _logger.LogInformation($"Going to revoke permission for {RoleNames.MpaGroup1} role for user: {userId}");

            try
            {
                await _revokePermissionForCommonEntitiesService.UpdateTaskConfigPermissionForMPAGroupUser(personId,userId);
                await _revokePermissionForCommonEntitiesService.UpdatePraxisTaskPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdateTrainingPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForEquipmentMaintenanceData(userId);
                await _revokePermissionForCommonEntitiesService.UpdateOpenItemConfigPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdateOpenItemPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemCompletionInfoData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTrainingAnswerData(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during revoke permission from data for {RoleNames.MpaGroup1} role for user: {userId}. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
            }
        }
    }
}
