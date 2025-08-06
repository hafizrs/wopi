using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionForEEGroupTwo : IRevokePermissionForRoleSpecific
    {
        private readonly ILogger<RevokePermissionForEEGroupTwo> _logger;
        private readonly IRevokePermissionForCommonEntities _revokePermissionForCommonEntitiesService;

        public RevokePermissionForEEGroupTwo(
            ILogger<RevokePermissionForEEGroupTwo> logger,
            IRevokePermissionForCommonEntities revokePermissionForCommonEntitiesService)
        {
            _logger = logger;
            _revokePermissionForCommonEntitiesService = revokePermissionForCommonEntitiesService;
        }
        public async Task RevokePermission(string userId, string personId)
        {
            _logger.LogInformation("Going to revoke permission for {RoleName} role for user: {UserId}", RoleNames.MpaGroup2, userId);

            try
            {
                await _revokePermissionForCommonEntitiesService.UpdateTrainingPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdateOpenItemConfigPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdateOpenItemPermissionForMPAGroupUser(personId, userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForOpenItemCompletionInfoData(userId);
                await _revokePermissionForCommonEntitiesService.UpdatePermissionForTrainingAnswerData(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during revoke permission from data for {RoleName} role for user: {UserId}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.", RoleNames.MpaGroup2, userId, ex.Message, ex.StackTrace);
            }
        }
    }
}
