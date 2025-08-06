using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataByRoleAndEntitySpecificStrategyService : IDeleteDataRoleAndEntitySpecificStrategy
    {
        private readonly DeleteRiskDataForSystemAdmin _deleteRiskDataForSystemAdmin;
        private readonly DeleteRiskDataForClientAdmin _deleteRiskDataForClientAdmin;
        private readonly DeleteAssessmentDataForSystemAdmin _deleteAssessmentDataForSystemAdmin;
        private readonly DeleteAssessmentDataForClientAdmin _deleteAssessmentDataForClientAdmin;
        private readonly DeleteCategoryDataForClientAdmin _deleteCategoryDataForClientAdmin;
        private readonly DeleteCategoryDataForSystemAdmin _deleteCategoryDataForSystemAdmin;
        private readonly DeleteSubCategoryDataForClientAdmin _deleteSubCategoryDataForClientAdmin;
        private readonly DeleteSubCategoryDataForSystemAdmin _deleteSubCategoryDataForSystemAdmin;
        private readonly DeleteFormCreatorDataAdminAndTaskController _deleteFormCreatorDataAdminAndTaskController;
        private readonly DeleteFormCreatorDataForClientAdmin _deleteFormCreatorDataForClientAdmin;
        private readonly DeleteUserDataAdminAndTaskController _deleteUserDataAdminAndTaskController;
        private readonly DeleteUserDataForClientAdmin _deleteUserDataForClientAdmin;

        public DeleteDataByRoleAndEntitySpecificStrategyService(
            DeleteRiskDataForSystemAdmin deleteRiskDataForSystemAdmin,
            DeleteRiskDataForClientAdmin deleteRiskDataForClientAdmin,
            DeleteAssessmentDataForSystemAdmin deleteAssessmentDataForSystemAdmin,
            DeleteAssessmentDataForClientAdmin deleteAssessmentDataForClientAdmin,
            DeleteCategoryDataForClientAdmin deleteCategoryDataForClientAdmin,
            DeleteCategoryDataForSystemAdmin deleteCategoryDataForSystemAdmin,
            DeleteSubCategoryDataForClientAdmin deleteSubCategoryDataForClientAdmin,
            DeleteSubCategoryDataForSystemAdmin deleteSubCategoryDataForSystemAdmin,
            DeleteFormCreatorDataAdminAndTaskController deleteFormCreatorDataAdminAndTaskController,
            DeleteFormCreatorDataForClientAdmin deleteFormCreatorDataForClientAdmin,
            DeleteUserDataAdminAndTaskController deleteUserDataAdminAndTaskController,
            DeleteUserDataForClientAdmin deleteUserDataForClientAdmin
            )
        {
            _deleteRiskDataForSystemAdmin = deleteRiskDataForSystemAdmin;
            _deleteRiskDataForClientAdmin = deleteRiskDataForClientAdmin;
            _deleteAssessmentDataForSystemAdmin = deleteAssessmentDataForSystemAdmin;
            _deleteAssessmentDataForClientAdmin = deleteAssessmentDataForClientAdmin;
            _deleteCategoryDataForClientAdmin = deleteCategoryDataForClientAdmin;
            _deleteCategoryDataForSystemAdmin = deleteCategoryDataForSystemAdmin;
            _deleteSubCategoryDataForClientAdmin = deleteSubCategoryDataForClientAdmin;
            _deleteSubCategoryDataForSystemAdmin = deleteSubCategoryDataForSystemAdmin;
            _deleteFormCreatorDataAdminAndTaskController = deleteFormCreatorDataAdminAndTaskController;
            _deleteFormCreatorDataForClientAdmin = deleteFormCreatorDataForClientAdmin;
            _deleteUserDataAdminAndTaskController = deleteUserDataAdminAndTaskController;
            _deleteUserDataForClientAdmin = deleteUserDataForClientAdmin;

        }
        public IDeleteDataByRoleSpecific GetDeleteType(string role, string entityName)
        {
            return entityName.ToUpper() switch
            {
                nameof(CollectionName.PRAXISRISK) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteRiskDataForSystemAdmin,
                    nameof(RoleType.TASK_CONTROLLER) => _deleteRiskDataForSystemAdmin,
                    nameof(RoleType.POWERUSER) => _deleteRiskDataForClientAdmin,
                    _ => null,
                },
                nameof(CollectionName.PRAXISASSESSMENT) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteAssessmentDataForSystemAdmin,
                    nameof(RoleType.POWERUSER) => _deleteAssessmentDataForClientAdmin,
                    _ => null,
                },
                nameof(CollectionName.PRAXISCLIENTCATEGORY) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteCategoryDataForSystemAdmin,
                    nameof(RoleType.TASK_CONTROLLER) => _deleteCategoryDataForSystemAdmin,
                    nameof(RoleType.POWERUSER) => _deleteCategoryDataForClientAdmin,
                    _ => null,
                },
                nameof(CollectionName.PRAXISCLIENTSUBCATEGORY) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteSubCategoryDataForSystemAdmin,
                    nameof(RoleType.TASK_CONTROLLER) => _deleteSubCategoryDataForSystemAdmin,
                    nameof(RoleType.POWERUSER) => _deleteSubCategoryDataForClientAdmin,
                    _ => null,
                },
                nameof(CollectionName.PRAXISFORM) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteFormCreatorDataAdminAndTaskController,
                    nameof(RoleType.TASK_CONTROLLER) => _deleteFormCreatorDataAdminAndTaskController,
                    nameof(RoleType.POWERUSER) => _deleteFormCreatorDataForClientAdmin,
                    _ => null,
                },
                nameof(CollectionName.USER) => role.ToUpper() switch
                {
                    nameof(RoleType.ADMIN) => _deleteUserDataAdminAndTaskController,
                    nameof(RoleType.TASK_CONTROLLER) => _deleteUserDataAdminAndTaskController,
                    nameof(RoleType.POWERUSER) => _deleteUserDataForClientAdmin,
                    _ => null,
                },
                _ => null,
            };
        }
    }
}
