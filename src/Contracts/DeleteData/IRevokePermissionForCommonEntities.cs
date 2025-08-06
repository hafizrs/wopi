using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IRevokePermissionForCommonEntities
    {
        Task UpdatePermissionForCategoryData(string userId);
        Task UpdatePermissionForFormData(string userId);
        Task UpdatePermissionForTrainingAnswerData(string userId);
        Task UpdatePermissionForTaskConfigData(string userId, string personId, string role);
        Task UpdatePermissionForPraxisTaskData(string userId, string personId, string role);
        Task UpdatePermissionForTaskSummaryData(string userId);
        Task UpdatePermissionForTaskScheduleData(string userId);
        Task UpdatePermissionForOpenItemConfigData(string userId);
        Task UpdatePermissionForOpenItemData(string userId);
        Task UpdatePermissionForRiskData(string userId);
        Task UpdatePermissionForRiskAssessmentData(string userId);
        Task UpdatePermissionForEquipmentData(string userId);
        Task UpdatePermissionForEquipmentMaintenanceData(string userId);
        Task UpdatePermissionForRoomData(string userId);
        Task UpdateTrainingPermissionForMPAGroupUser(string personId, string userId);
        Task UpdateOpenItemConfigPermissionForMPAGroupUser(string personId, string userId);
        Task UpdateOpenItemPermissionForMPAGroupUser(string personId, string userId);
        Task UpdatePermissionForOpenItemCompletionInfoData(string userId);
        Task UpdateTaskConfigPermissionForMPAGroupUser(string personId, string userId);
        Task UpdatePraxisTaskPermissionForMPAGroupUser(string personId, string userId);
    }
}
