using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataStrategyService : IDeleteDataStrategy
    {
        private readonly DeleteDataForPraxisOrganization _deleteDataForPraxisOrganization;
        private readonly DeleteDataForEquipment _deleteDataForEquipment;
        private readonly DeleteDataForLocation _deleteDataForLocation;
        private readonly DeleteDataForEquipmentMaintenance _deleteDataForEquipmentMaintenance;
        private readonly DeleteDataForTraining _deleteDataForTraining;
        private readonly DeleteDataForRiskManagement _deleteDataForRiskManagement;
        private readonly DeleteDataForPraxisAssessment _deleteDataForPraxisAssessment;
        private readonly DeleteCategoryFromPraxisClientCategory _deleteCategoryFromPraxisClientCategory;
        private readonly DeleteSubCategoryFromPraxisClientCategory _deleteSubCategoryFromPraxisClientCategory;
        private readonly DeleteDataForFormCreator _deleteDataForFormCreator;
        private readonly DeleteDataForUser _deleteDataForUser;
        private readonly DeleteDataForProcessGuide _deleteDataForProcessGuide;
        private readonly DeleteDataForPraxisClient _deleteDataForPraxisClient;
        private readonly DeleteDataForPraxisReport _deleteDataForPraxisReport; 

        public DeleteDataStrategyService(
            DeleteDataForPraxisOrganization deleteDataForPraxisOrganization,
            DeleteDataForEquipment deleteDataForEquipment,
            DeleteDataForLocation deleteDataForLocation,
            DeleteDataForEquipmentMaintenance deleteDataForEquipmentMaintenance,
            DeleteDataForTraining deleteDataForTraining,
            DeleteDataForRiskManagement deleteDataForRiskManagement,
            DeleteDataForPraxisAssessment deleteDataForPraxisAssessment,
            DeleteCategoryFromPraxisClientCategory deleteCategoryFromPraxisClientCategory,
            DeleteSubCategoryFromPraxisClientCategory deleteSubCategoryFromPraxisClientCategory,
            DeleteDataForFormCreator deleteDataForFormCreator,
            DeleteDataForUser deleteDataForUser,
            DeleteDataForProcessGuide deleteDataForProcessGuide,
            DeleteDataForPraxisClient deleteDataForPraxisClient,
            DeleteDataForPraxisReport deleteDataForPraxisReport)
        {
            _deleteDataForPraxisOrganization = deleteDataForPraxisOrganization;
            _deleteDataForEquipment = deleteDataForEquipment;
            _deleteDataForLocation = deleteDataForLocation;
            _deleteDataForEquipmentMaintenance = deleteDataForEquipmentMaintenance;
            _deleteDataForTraining = deleteDataForTraining;
            _deleteDataForRiskManagement = deleteDataForRiskManagement;
            _deleteDataForPraxisAssessment = deleteDataForPraxisAssessment;
            _deleteCategoryFromPraxisClientCategory = deleteCategoryFromPraxisClientCategory;
            _deleteSubCategoryFromPraxisClientCategory = deleteSubCategoryFromPraxisClientCategory;
            _deleteDataForFormCreator = deleteDataForFormCreator;
            _deleteDataForUser = deleteDataForUser;
            _deleteDataForProcessGuide = deleteDataForProcessGuide;
            _deleteDataForPraxisClient = deleteDataForPraxisClient;
            _deleteDataForPraxisReport = deleteDataForPraxisReport;
        }

        public IDeleteDataByCollectionSpecific GetDeleteType(string entityName)
        {
            switch (entityName.ToUpper())
            {
                case nameof(CollectionName.PRAXISORGANIZATION):
                    return _deleteDataForPraxisOrganization;
                case nameof(CollectionName.PRAXISEQUIPMENT):
                    return _deleteDataForEquipment;
                case nameof(CollectionName.PRAXISROOM):
                    return _deleteDataForLocation;
                case nameof(CollectionName.PRAXISEQUIPMENTMAINTENANCE):
                    return _deleteDataForEquipmentMaintenance;
                case nameof(CollectionName.PRAXISTRAINING):
                    return _deleteDataForTraining;
                case nameof(CollectionName.PRAXISRISK):
                    return _deleteDataForRiskManagement;
                case nameof(CollectionName.PRAXISASSESSMENT):
                    return _deleteDataForPraxisAssessment;
                case nameof(CollectionName.PRAXISCLIENTCATEGORY):
                    return _deleteCategoryFromPraxisClientCategory;
                case nameof(CollectionName.PRAXISCLIENTSUBCATEGORY):
                    return _deleteSubCategoryFromPraxisClientCategory;
                case nameof(CollectionName.PRAXISFORM):
                    return _deleteDataForFormCreator;
                case nameof(CollectionName.USER):
                    return _deleteDataForUser;
                case nameof(CollectionName.PRAXISPROCESSGUIDE):
                    return _deleteDataForProcessGuide;
                case nameof(CollectionName.PRAXISCLIENT):
                case nameof(CollectionName.PRAXISUSERADDITIONALINFO):
                    return _deleteDataForPraxisClient;
                case nameof(CollectionName.PRAXISREPORT):
                    return _deleteDataForPraxisReport;
                default:
                    return null;

            }
        }
    }
}
