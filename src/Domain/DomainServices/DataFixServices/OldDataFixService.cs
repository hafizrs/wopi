using Aspose.Pdf.LogicalStructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class OldDataFixService : IResolveProdDataIssuesService
    {
        private readonly ILogger<OldDataFixService> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisFileService _praxisFileService;
        private readonly IPraxisAssignedTaskFormService _praxisAssignedTaskFormService;
        private readonly IPraxisFormService _praxisFormService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly IUpdateClientSubscriptionInformation _updateClientSubscriptionInformation;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly IAppCatalogueRepositoryService _appCatalogueRepositoryService;
        private readonly IReportingTaskCockpitSummaryCommandService _reportingTaskCockpitSummaryCommandService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ITranslationService _translationService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IPraxisEquipmentService _praxisEquipmentService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;

        public OldDataFixService(
            ILogger<OldDataFixService> logger,
            IRepository repository,
            IPraxisFileService praxisFileService,
            IPraxisFormService praxisFormService,
            IPraxisAssignedTaskFormService praxisAssignedTaskFormService,
            IGenericEventPublishService genericEventPublishService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            IUpdateClientSubscriptionInformation updateClientSubscriptionInformation,
            IMongoClientRepository mongoClientRepository,
            IAppCatalogueRepositoryService appCatalogueRepositoryService,
            IReportingTaskCockpitSummaryCommandService reportingTaskCockpitSummaryCommandService,
            ISecurityContextProvider securityContextProvider,
            IOrganizationSubscriptionService organizationSubscriptionService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ITranslationService translationService,
            IUserCountMaintainService userCountMaintainService,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IPraxisEquipmentService praxisEquipmentService,
            IObjectArtifactShareService objectArtifactShareService)
        {
            _logger = logger;
            _repository = repository;
            _praxisFileService = praxisFileService;
            _praxisFormService = praxisFormService;
            _praxisAssignedTaskFormService = praxisAssignedTaskFormService;
            _genericEventPublishService = genericEventPublishService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _updateClientSubscriptionInformation = updateClientSubscriptionInformation;
            _mongoClientRepository = mongoClientRepository;
            _appCatalogueRepositoryService = appCatalogueRepositoryService;
            _reportingTaskCockpitSummaryCommandService = reportingTaskCockpitSummaryCommandService;
            _securityContextProvider = securityContextProvider;
            _organizationSubscriptionService = organizationSubscriptionService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _translationService = translationService;
            _userCountMaintainService = userCountMaintainService;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _praxisEquipmentService = praxisEquipmentService;
            _objectArtifactShareService = objectArtifactShareService;
        }

        public async Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            _logger.LogInformation("Entered service: {ServiceName}", nameof(OldDataFixService));
            try
            {
                switch (command.FunctionName)
                {
                    case "SeedForStgToProd_26_9_24":
                        await SeedForStgToProd_26_9_24(command);
                        break;
                    case "FixPraxisClientCategoryForOrgId":
                        await FixPraxisClientCategoryForOrgId();
                        break;
                    case "FixPraxisEquipmentMaintenanceFilesAndEndDate":
                        await FixPraxisEquipmentMaintenanceFilesAndEndDate();
                        break;
                    case "FixPraxisProcessGuidePermissions":
                        await FixPraxisProcessGuidePermissions();
                        break;
                    case "CreateAssignedTaskForm":
                        await CreateAssignedTaskForm();
                        break;
                    case "FixCockpitSummaryData":
                        await FixCockpitSummaryData();
                        break;
                    case "FixDocumentEditMappingFileType":
                        await FixDocumentEditMappingFileType();
                        break;
                    case "MarkedToDeletePraxisFormIncludingDependantEntites":
                        await MarkedToDeletePraxisFormIncludingDependantEntites(command.Payload);
                        break;
                    case "FixPraxisClientSubscription":
                        await FixPraxisClientSubscription(command.Payload);
                        break;
                    case "FixArtifactsMetaDataIsUsedInAnotherEntityProperty":
                        await FixArtifactsMetaDataIsUsedInAnotherEntityProperty();
                        break;
                    case "FixArtifactReferenceUsage":
                        FixArtifactUsageReference();
                        break;
                    case "AddUilmApplications":
                        await AddUilmApplications(command);
                        break;
                    case "FixExpiredButNotRenewedSubscriptions":
                        await FixExpiredButNotRenewedSubscriptions();
                        break;
                    case "FixDmsArtifactUsageReferenceMetadata":
                        await FixDmsArtifactUsageReferenceMetadata();
                        break;
                    case "UpdateSubscriptionForSpecificOrgUnit":
                        await UpdateSubscriptionForSpecificOrgUnit(command.Payload);
                        break;
                    case "FixNativeLanguageForPraxisUser":
                        await FixNativeLanguageForPraxisUser();
                        break;
                    case "FixDynamicNavigationForEEB":
                        await FixDynamicNavigationForEEB();
                        break;
                    case "FixEquipmentMaintenanceDates":
                        await FixEquipmentMaintenanceDates();
                        break;
                    case "UnsetCirsUnusedFields":
                        await UnsetCirsUnusedFields();
                        break;
                    case "FixFeatureRoleMapData":
                        await FixFeatureRoleMapData();
                        break;
                    case "FixClientAdditionalInfoData":
                        await FixClientAdditionalInfoData();
                        break;
                    case "FixProcessGuideListingOfEquipment":
                        await FixProcessGuideListingOfEquipment();
                        break;
                    case "FixContactPersonsOfEquipment":
                        await FixContactPersonsOfEquipment();
                        break;
                    case "UnsetCockpitUnusedFields":
                        await UnsetCockpitUnusedFields();
                        break;
                    case "FixPraxisClientSubscriptionForProd":
                        await FixPraxisClientSubscriptionForProd();
                        break;
                    case "SetCockpitDocumentObjectSummaryActiveStatus":
                        await SetCockpitDocumentObjectSummaryActiveStatus();
                        break;
                    case "SetCompanyContactInfoInEquipmentDetails":
                        await SetCompanyContactInfoInEquipmentDetails();
                        break;
                    case "SetClientIdInDmsArtifactReference":
                        await SetClientIdInDmsArtifactReference();
                        break;
                    case "SetDueDateInCockpitProcessGuide":
                        await SetDueDateInCockpitProcessGuide();
                        break;
                    case "SetClientIdInCockpitDocumentSummary":
                        await SetClientIdInCockpitDocumentSummary();
                        break;
                    case "SetClientIdInReportingCockpitSummaryDependentTask":
                        await SetClientIdInReportingCockpitSummaryDependentTask();
                        break;
                    case "MarkToDeleteCockpitData":
                        await MarkToDeleteCockpitData();
                        break;
                    case "SubscriptionUpdateForSystemAdmin":
                        await SubscriptionUpdateForSystemAdmin(command);
                        break;
                    case "DeleteAllDataForRiqsAIConversation":
                        await DeleteAllDataForRiqsAIConversation();
                        break;
                    case "FixRiqsPediaDeletedUsers":
                        await FixRiqsPediaDeletedUsers(command.Payload);
                        break;
                    case "RemoveDeletedRiqsPediaFilesFromEverywhere":
                        await RemoveDeletedRiqsPediaFilesFromEverywhere();
                        break;
                    case "RemoveCockpitDocumentTaskOfStandardPrinciple":
                        await RemoveCockpitDocumentTaskOfStandardPrinciple();
                        break;
                    case "FixTaskCreatorInReportingCockpitSummary":
                        await FixTaskCreatorInReportingCockpitSummary();
                        break;
                    case "FixSerialNumberInMetaDataListForEquipment":
                        await FixSerialNumberInMetaDataListForEquipment();
                        break;
                    case "FixPraxisFormForDeptUser":
                        await FixPraxisFormForDeptUser();
                        break;
                    case "FixMaintenanceDatesForForEquipment":
                        await FixMaintenanceDatesForForEquipment();
                        break;
                    case "AddProcessGuideAnswerMetaData":
                        await AddProcessGuideAnswerMetaData();
                        break;
                    case "FixObjectArtifactPermissions":
                        await FixObjectArtifactPermissions();
                        break;
                    case "FixNonStandardLibraryRights":
                        await FixNonStandardLibraryRights();
                        break;
                    case "FixTranslationInResourceKeys":
                        await FixTranslationInResourceKeys(command.Payload);
                        break;
                    case "FixRightsToEquipmentTable":
                        await FixRightsToEquipmentTable();
                        break;
                    case "FixObjectArifactSortableField":
                        await FixObjectArifactSortableField();
                        break;
                    case "FixUserCount":
                        await FixUserCount();
                        break;
                    case "FixEquipmentLocationLogRemarks":
                        await FixEquipmentLocationLogRemarks();
                        break;
                    case "FixProcessGuideDates":
                        await FixProcessGuideDates();
                        break;
                    case "FixArtifactWritePermission":
                        await FixArtifactWritePermission();
                        break;
                    case "FixEquipmentQrCode":
                        await FixEquipmentQrCode();
                        break;
                    case "FixPraxisFormOwnerRole":
                        await FixPraxisFormOwnerRole();
                        break;
                    case "FixPraxisFormPermission":
                        FixPraxisFormPermission();
                        break;
                    case "RemoveDeletedCirsCockpitTask":
                        await RemoveDeletedCirsCockpitTask(command.Payload);
                        break;
                    case "FixPraxisActivePraxisUser":
                        FixPraxisActivePraxisUser();
                        break;
                    case "FixPraxisFormSharedOrgProperty":
                        await FixPraxisFormSharedOrgProperty();
                        break;
                    case "FixNextRequiredStatusForGeneratedReportTemplate":
                        await FixNextRequiredStatusForGeneratedReportTemplate();
                        break;
                    case "FixLastUpdatedByPropertyInCockpitSummary":
                        await FixLastUpdatedByPropertyInCockpitSummary();
                        break;
                    default:
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(OldDataFixService), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task FixFeatureRoleMapData()
        {
            try
            {

                var departmentIds = _repository.GetItems<PraxisClient>().Select(c => c.ItemId).ToList();

                foreach (var departmentId in departmentIds)
                {

                    var navRoleList = new List<string>
                    {
                        $"{RoleNames.PowerUser_Nav}_{departmentId}",
                        $"{RoleNames.Leitung_Nav}_{departmentId}",
                        $"{RoleNames.MpaGroup1_Nav}_{departmentId}",
                        $"{RoleNames.MpaGroup2_Nav}_{departmentId}"
                    };


                    var featureRoleMaps =
                        _appCatalogueRepositoryService.GetFeatureRoleMapsByRoles(navRoleList).ToList();
                    foreach (var featureRoleMap in featureRoleMaps)
                    {
                        await _repository.DeleteAsync<FeatureRoleMap>(f => f.ItemId == featureRoleMap.ItemId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occurred in FixFeatureRoleMapData. Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}.");
            }
        }


        private async Task SeedForStgToProd_26_9_24(ResolveProdDataIssuesCommand command)
        {
            await FixPraxisEquipmentMaintenanceFilesAndEndDate();
            await FixPraxisClientCategoryForOrgId();
            await FixPraxisProcessGuidePermissions();
            await CreateAssignedTaskForm();
            await FixDocumentEditMappingFileType();
            FixArtifactUsageReference();
            await FixExpiredButNotRenewedSubscriptions();
        }

        private async Task<bool> FixPraxisClientCategoryForOrgId()
        {
            try
            {
                var categories = _repository.GetItems<PraxisClientCategory>(pu => !pu.IsMarkedToDelete)?.ToList();
                foreach (var category in categories)
                {
                    if (!string.IsNullOrEmpty(category.ClientId))
                    {
                        var orgId = _repository.GetItem<PraxisClient>(c => c.ItemId == category.ClientId)
                            ?.ParentOrganizationId;
                        category.OrganizationId = orgId;
                        await _repository.UpdateAsync(c => c.ItemId == category.ItemId, category);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixPraxisClientCategoryForOrgId), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> UpdatePraxisClient(PraxisClient praxisClient)
        {
            await _repository.UpdateAsync(c => c.ItemId == praxisClient.ItemId, praxisClient);
            return true;
        }

        private PraxisOrganization GetOrganizationDataById(string orgId)
        {
            var orgData = _repository.GetItem<PraxisOrganization>(o => !o.IsMarkedToDelete && o.ItemId == orgId);
            return orgData;
        }

        private async Task FixPraxisProcessGuidePermissions()
        {
            try
            {
                var pgConfigs = _repository.GetItems<PraxisProcessGuideConfig>(p => !p.IsMarkedToDelete)?.ToList();
                if (pgConfigs != null)
                {
                    foreach (var pg in pgConfigs)
                    {

                        pg.RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser };
                        pg.RolesAllowedToUpdate = new string[]
                            { RoleNames.Admin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung };
                        pg.RolesAllowedToDelete = new string[] { RoleNames.Admin, RoleNames.AppUser };
                        await _repository.UpdateAsync(p => p.ItemId == pg.ItemId, pg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixPraxisProcessGuidePermissions), ex.Message, ex.StackTrace);
            }
        }

        private async Task CreateAssignedTaskForm()
        {
            try
            {
                var pgList = _repository.GetItems<PraxisProcessGuide>()?.ToList();

                if (pgList != null)
                {
                    foreach (var pg in pgList)
                    {
                        await Task.Run(() =>
                        {
                            _praxisAssignedTaskFormService.CreateAssignedForm(pg.FormId, nameof(PraxisProcessGuide),
                                pg.ItemId);
                        });


                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName} from PraxisFrom. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(CreateAssignedTaskForm), ex.Message, ex.StackTrace);
            }
        }

        private async Task FixPraxisEquipmentMaintenanceFilesAndEndDate()
        {
            try
            {
                var equipmentMaintenanceList = GetNonDeletedEquipmentMaintenanceItems();
                foreach (var equipment in equipmentMaintenanceList)
                {
                    await ProcessEquipmentMaintenanceItems(equipment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixPraxisEquipmentMaintenanceFilesAndEndDate), ex.Message, ex.StackTrace);
            }
        }

        private async Task ProcessEquipmentMaintenanceItems(PraxisEquipmentMaintenance equipment)
        {
            try
            {
                _logger.LogInformation(
                    $"Enter to fix the Equipment Maintenance Files and End Date info for Id: {equipment.ItemId}");

                UpdateEquipmentMaintenanceItem(equipment);
                await UpdateRepositoryAsync(equipment);

                _logger.LogInformation(
                    $"Equipment Maintenance Files and End Date info Successfully updated for ID: {equipment.ItemId}");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(ProcessEquipmentMaintenanceItems), e.Message, e.StackTrace);
            }
        }

        private void UpdateEquipmentMaintenanceItem(PraxisEquipmentMaintenance equipment)
        {
            equipment.Answers = equipment.Answers?
                .Select(GetEquipmentMaintenanceAnswer)
                .ToList() ?? new List<EquipmentMaintenanceAnswer>();

            equipment.ExternalUserInfos = equipment.ExternalUserInfos?
                .Select(info =>
                {
                    if (info.Answer != null)
                        info.Answer = GetEquipmentMaintenanceAnswer(info.Answer);
                    return info;
                })
                .ToList() ?? new List<PraxisEquipmentMaintenanceByExternalUser>();
            UpdateEquipmentMaintenanceEndDateAndPeriod(equipment);
        }

        private void UpdateEquipmentMaintenanceEndDateAndPeriod(PraxisEquipmentMaintenance equipment)
        {
            if (string.IsNullOrEmpty(equipment.ScheduleType))
            {
                equipment.ScheduleType = "MAINTENANCE";
            }

            if (equipment.MaintenanceEndDate.Year >= 2000) return;
            if (equipment.MaintenancePeriod <= 1)
            {
                equipment.MaintenanceEndDate = equipment.MaintenanceDate.Date;
                equipment.MaintenancePeriod = 1;
                equipment.MaintenanceDate = equipment.MaintenanceDate.Date.AddDays(-1);
            }
        }

        private async Task UpdateRepositoryAsync(PraxisEquipmentMaintenance equipment)
        {
            await _repository.UpdateAsync(eq => eq.ItemId.Equals(equipment.ItemId), equipment);
        }

        private List<PraxisEquipmentMaintenance> GetNonDeletedEquipmentMaintenanceItems()
        {
            return _repository
                .GetItems<PraxisEquipmentMaintenance>(equipment => !equipment.IsMarkedToDelete)?
                .AsEnumerable()
                .Where(GetValidityCheck)
                .ToList() ?? new List<PraxisEquipmentMaintenance>();
        }

        private bool GetValidityCheck(PraxisEquipmentMaintenance equipment)
        {
            return (equipment.Answers != null &&
                    equipment.Answers.Any()) ||
                   (equipment.ExternalUserInfos != null &&
                    equipment.ExternalUserInfos.Any()) ||
                   equipment.MaintenanceEndDate.Year < 2000 ||
                   string.IsNullOrEmpty(equipment.ScheduleType);
        }

        private EquipmentMaintenanceAnswer GetEquipmentMaintenanceAnswer(EquipmentMaintenanceAnswer answer)
        {
            answer.Files = GetPraxisDocuments(answer.FileId);
            if (answer.ApprovalResponse != null)
                answer.ApprovalResponse.Files = GetPraxisDocuments(answer.ApprovalResponse.FileId);
            return answer;
        }

        private List<PraxisDocument> GetPraxisDocuments(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId)) return new List<PraxisDocument>();

            var file = _praxisFileService.GetFileInformation(fileId);
            if (file == null) return new List<PraxisDocument>();

            var praxisDocument = new PraxisDocument
            {
                DocumentId = file.ItemId,
                DocumentName = file.Name,
                CreatedOn = file.CreateDate,
                IsDeleted = false,
                IsUploadedFromWeb = true
            };
            return new List<PraxisDocument> { praxisDocument };
        }


        private async Task FixCockpitSummaryData()
        {
            var cockpitSummaryList = _repository
                .GetItems<RiqsTaskCockpitSummary>(c =>
                    !c.IsMarkedToDelete && c.RelatedEntityName == CockpitTypeNameEnum.PraxisTraining)?
                .ToList() ?? new List<RiqsTaskCockpitSummary>();
            foreach (var riqsTaskCockpitSummary in cockpitSummaryList)
            {
                riqsTaskCockpitSummary.IsSummaryHidden = riqsTaskCockpitSummary.IsAllUserAutoSelected;
                await _repository.UpdateAsync(c => c.ItemId == riqsTaskCockpitSummary.ItemId, riqsTaskCockpitSummary);
            }
        }

        private async Task FixDocumentEditMappingFileType()
        {
            var documentEditMappingRecords = _repository
                .GetItems<DocumentEditMappingRecord>(r => !r.IsMarkedToDelete)?
                .ToList() ?? new List<DocumentEditMappingRecord>();
            var fileTypeKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.FILE_TYPE)];
            var tasks = documentEditMappingRecords.Select(async record =>
            {
                var artifact = await _repository.GetItemAsync<ObjectArtifact>(a => a.ItemId == record.ObjectArtifactId);

                if (artifact == null) record.FileType = string.Empty;
                else if (artifact.MetaData == null) record.FileType = string.Empty;
                else if (!artifact.MetaData.TryGetValue(fileTypeKey, out var fileType)) record.FileType = string.Empty;
                else record.FileType = fileType.Value;
                Console.WriteLine($"ItemId = {artifact?.ItemId} - FileType = {record.FileType}");

                await _repository.UpdateAsync(r => r.ItemId == record.ItemId, record);

            });

            await Task.WhenAll(tasks);
        }

        private async Task AddUilmApplications(ResolveProdDataIssuesCommand command)
        {
            if (command?.Payload?.UilmApplications?.Count > 0)
            {
                foreach (var app in command.Payload.UilmApplications)
                {
                    var exist = await _repository.ExistsAsync<UilmApplication>(o => o.ItemId == app.ItemId);
                    if (!exist)
                    {
                        await _repository.SaveAsync(app);
                    }
                }
            }
        }

        private async Task FixExpiredButNotRenewedSubscriptions()
        {
            var orgIds =
                _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete)?.Select(o => o.ItemId)?.ToList() ??
                new List<string>();
            var subs = _repository.GetItems<PraxisClientSubscription>
            (c => c.SubscritionStatus == nameof(PraxisEnums.EXPIRED) && !string.IsNullOrEmpty(c.OrganizationId) &&
                  orgIds.Contains(c.OrganizationId) && !c.IsMarkedToDelete).ToList();

            foreach (var sub in subs)
            {
                var activeSub = await _repository.GetItemAsync<PraxisClientSubscription>(c =>
                    !c.IsMarkedToDelete && c.IsLatest && c.IsActive && c.OrganizationId == sub.OrganizationId);
                if (activeSub == null)
                {
                    sub.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    sub.IsActive = true;
                    sub.IsLatest = true;
                    await _repository.UpdateAsync(s => s.ItemId == sub.ItemId, sub);
                }

                var notif = await _repository.GetItemAsync<PraxisClientSubscriptionNotification>(c =>
                    !c.IsMarkedToDelete && c.IsActive && c.OrganizationId == sub.OrganizationId);
                if (notif == null)
                {
                    notif = await _repository.GetItemAsync<PraxisClientSubscriptionNotification>(c =>
                        !c.IsMarkedToDelete && c.OrganizationId == sub.OrganizationId);
                    if (notif != null)
                    {
                        notif.IsActive = true;
                        await _repository.UpdateAsync(s => s.ItemId == notif.ItemId, notif);
                    }
                }
            }
        }

        private async Task MarkedToDeletePraxisFormIncludingDependantEntites(ResolveProdDataIssuesPayload command)
        {
            var clientIds = new List<string>();
            if (command?.DepartmentIds?.Count > 0)
            {
                clientIds.AddRange(command.DepartmentIds);
            }

            var forms = _repository.GetItems<PraxisForm>(
                f => clientIds.Contains(f.ClientId) ||
                     (f.ClientInfos != null && f.ClientInfos.Any(c => clientIds.Contains(c.ClientId))) ||
                     (
                         f.ProcessGuideCheckList != null && f.ProcessGuideCheckList.Any(p =>
                             clientIds.Contains(p.ClientId) ||
                             (p.ClientInfos != null && p.ClientInfos.Any(p => clientIds.Contains(p.ClientId)))
                         )
                     )
            )?.ToList();

            var listOfTasks = new List<Task>();
            foreach (var form in forms)
            {
                listOfTasks.Add(_praxisFormService.MarkedToDeletePraxisFormIncludingDependantEntites(form.ItemId));
            }

            await Task.WhenAll(listOfTasks);
        }

        private async Task FixPraxisClientSubscription(ResolveProdDataIssuesPayload command)
        {
            var orgs = _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete)?.ToList() ??
                       new List<PraxisOrganization>();
            foreach (var org in orgs)
            {
                var subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(org.ItemId);
                if (subs == null)
                    await _praxisClientSubscriptionService.SaveClientSubscriptionOnOrgCreateUpdate(org.ItemId);
                else
                {
                    subs.NumberOfAuthorizedUsers = org.UserLimit * 2;
                    subs.TokenSubscription = subs.TokenSubscription ?? new TokenSubscriptionInfo()
                    {
                        TotalAdditionalTokenCost = 0,
                        IncludedTokenInMillion = 0,
                        PricePerMillionToken = 0,
                        TotalAdditionalTokenInMillion = 0
                    };
                    subs.IsTokenApplied = false;
                    subs.OrganizationName = org.ClientName;
                    subs.OrganizationEmail = org.ContactEmail;
                    await _repository.UpdateAsync(c => c.ItemId == subs.ItemId, subs);
                }
            }

            var depts = _repository.GetItems<PraxisClient>(o => !o.IsMarkedToDelete)?.ToList() ??
                        new List<PraxisClient>();
            var listOftasks = new List<Task>();
            foreach (var dept in depts)
            {
                listOftasks.Add(
                    _praxisClientSubscriptionService.SaveClientSubscriptionOnClientCreateUpdate(dept.ItemId));
            }

            await Task.WhenAll(listOftasks);

            foreach (var dept in depts)
            {
                dept.AuthorizedUserLimit = dept.UserLimit * 2;
                await _repository.UpdateAsync(c => c.ItemId == dept.ItemId, dept);
            }

            foreach (var org in orgs)
            {
                org.AuthorizedUserLimit = org.UserLimit * 2;
                org.TotalDepartmentAuthorizedUserLimit = org.TotalDepartmentUserLimit * 2;
                await _repository.UpdateAsync(c => c.ItemId == org.ItemId, org);
            }
        }

        private async Task FixArtifactsMetaDataIsUsedInAnotherEntityProperty()
        {
            var isUsedInAnotherEntityKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    nameof(ObjectArtifactMetaDataKeyEnum.IS_USED_IN_ANOTHER_ENTITY)];
            var artifactUsageReferenceCounterKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    nameof(ObjectArtifactMetaDataKeyEnum.ARTIFACT_USAGE_REFERENCE_COUNTER)];

            var artifacts = _repository
                .GetItems<ObjectArtifact>(a =>
                    !a.IsMarkedToDelete &&
                    a.MetaData != null &&
                    a.MetaData.ContainsKey(isUsedInAnotherEntityKey))?
                .ToList() ?? new List<ObjectArtifact>();

            var tasks = artifacts.Select(async artifact =>
            {
                var currentValue = artifact.MetaData.TryGetValue(artifactUsageReferenceCounterKey, out var value)
                    ? value.Value
                    : "0";
                var updatedValue = currentValue != "0"
                    ? ((int)LibraryBooleanEnum.TRUE).ToString()
                    : ((int)LibraryBooleanEnum.FALSE).ToString();

                artifact.MetaData[isUsedInAnotherEntityKey].Value = updatedValue;
                await _repository.UpdateAsync(a => a.ItemId == artifact.ItemId, artifact);
            });

            await Task.WhenAll(tasks);
        }

        private void FixArtifactUsageReference()
        {
            FixArtifactUsageReferenceForPraxisForm();
            FixArtifactUsageReferenceForPraxisOpenItem();
        }

        private void FixArtifactUsageReferenceForPraxisForm()
        {
            var purposeFormKeysWithArtifacts = new[] { "process-guide", "training-module" };
            var forms = _repository.GetItems<PraxisForm>(f =>
                    !f.IsMarkedToDelete &&
                    f.PurposeOfFormKey != null &&
                    purposeFormKeysWithArtifacts.Contains(f.PurposeOfFormKey))?
                .ToList() ?? new List<PraxisForm>();
            forms.ForEach(form => _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(form));
        }

        private void FixArtifactUsageReferenceForPraxisOpenItem()
        {
            var openItems = _repository.GetItems<PraxisOpenItem>(o =>
                    !o.IsMarkedToDelete)?
                .ToList() ?? new List<PraxisOpenItem>();
            openItems.ForEach(openItem => _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(openItem));
        }

        private async Task FixDmsArtifactUsageReferenceMetadata()
        {
            var references = _repository.GetItems<DmsArtifactUsageReference>(a =>
                    !a.IsMarkedToDelete &&
                    a.MetaData == null)?
                .ToList() ?? new List<DmsArtifactUsageReference>();

            var task = references.Select(async reference =>
            {
                var metaData = GetMetaData(reference.RelatedEntityId, reference.RelatedEntityName);
                reference.MetaData = metaData;
                await _repository.UpdateAsync(a => a.ItemId == reference.ItemId, reference);
            });
            await Task.WhenAll(task);
        }

        private async Task FixDynamicNavigationForEEB()
        {
            var depts = _repository.GetItems<PraxisClient>(c => !c.IsMarkedToDelete).ToList();
            foreach (var dept in depts)
            {
                var navMp1 = $"{RoleNames.MpaGroup1_Nav}_{dept.ItemId}";
                var navMp2 = $"{RoleNames.MpaGroup2_Nav}_{dept.ItemId}";
                var roleMap1 = await _repository.GetItemAsync<FeatureRoleMap>
                    (o => o.FeatureId == "EquipmentManagement.Navigation" && o.RoleName == navMp1);
                var roleMap2 = await _repository.GetItemAsync<FeatureRoleMap>
                    (o => o.FeatureId == "EquipmentManagement.Navigation" && o.RoleName == navMp2);
                if (roleMap1 != null && roleMap2 == null)
                {
                    roleMap1.RoleName = navMp2;
                    roleMap1.ItemId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                    await _repository.SaveAsync(roleMap1);
                }
            }
        }


        private async Task FixEquipmentMaintenanceDates()
        {
            var equipments = _repository
                .GetItems<PraxisEquipment>(e => !e.IsMarkedToDelete).ToList();
            foreach (var eq in equipments)
            {
                var maintainanceList = _repository
                    .GetItems<PraxisEquipmentMaintenance>(m =>
                        m.PraxisEquipmentId == eq.ItemId && !m.IsMarkedToDelete).ToList();

                eq.MaintenanceDates = maintainanceList?.Select(m => new MaintenanceDateProp()
                {
                    ItemId = m.ItemId,
                    Date = m.MaintenanceEndDate,
                    CompletionStatus = m.CompletionStatus
                })?.OrderBy(m => m.Date)?.ToList() ?? new List<MaintenanceDateProp>();
                await _repository.UpdateAsync(e => e.ItemId == eq.ItemId, eq);
            }
        }

        private async Task UnsetCirsUnusedFields()
        {
            var collection = _mongoClientRepository.GetCollection(nameof(CirsGenericReport));
            var filter = Builders<BsonDocument>.Filter.Empty;

            var update = Builders<BsonDocument>.Update
                .Unset("ProcessGuideAttachment")
                .Unset("OpenItemAttachment");

            var result = await collection.UpdateManyAsync(filter, update);
        }

        private Dictionary<string, MetaValuePair> GetMetaData(string itemId, string entityName)
        {
            var metaData = new Dictionary<string, MetaValuePair>();
            if (entityName != nameof(CirsGenericReport)) return metaData;
            var report = _repository.GetItem<CirsGenericReport>(r => r.ItemId == itemId);
            metaData.Add("CirsDashboardName",
                new MetaValuePair { Type = nameof(String), Value = report.CirsDashboardName.ToString() });
            return metaData;
        }

        private async Task UpdateSubscriptionForSpecificOrgUnit(ResolveProdDataIssuesPayload command)
        {
            var orgs = _repository
                .GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete && command.OrganizationIds.Contains(o.ItemId))
                ?.ToList() ?? new List<PraxisOrganization>();
            foreach (var org in orgs)
            {
                var subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(org.ItemId);
                org.CreateDate = DateTime.UtcNow;
                var orgSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow.ToLocalTime(),
                    DurationOfSubscription = 12,
                    PaymentMethod = subs?.PaymentMethod ?? "Annually",
                    BillingAddress = subs?.BillingAddress,
                    ResponsiblePerson = subs?.ResponsiblePerson,
                    Location = subs?.Location ?? "CH",
                    PerUserCost = subs?.PerUserCost ?? 0,
                    AverageCost = subs?.AverageCost ?? 0,
                    TaxDeduction = subs?.TaxDeduction ?? 0,
                    GrandTotal = subs?.GrandTotal ?? 0,
                    PaymentCurrency = subs?.PaymentCurrency ?? "chf",
                    PaidAmount = subs?.PaidAmount ?? 0,
                    IsTokenApplied = subs?.IsTokenApplied ?? command.AdditionalToken > 0,
                    IsManualTokenApplied = subs?.IsManualTokenApplied ?? command.AdditionalManualToken > 0,
                    IsActive = true,
                    IsLatest = true,
                    StorageSubscription = new StorageSubscriptionInfo
                    {
                        IncludedStorageInGigaBites = org.UserLimit * .5,
                        TotalAdditionalStorageInGigaBites =
                            subs?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0.0,
                        TotalAdditionalStorageCost = subs?.StorageSubscription?.TotalAdditionalStorageCost ?? 0.0
                    },
                    TokenSubscription = new TokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = subs?.TokenSubscription?.IncludedTokenInMillion ?? 0.0,
                        TotalAdditionalTokenInMillion = subs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalAdditionalTokenCost = subs?.TokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    ManualTokenSubscription = new ManualTokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = subs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0.0,
                        TotalAdditionalTokenInMillion =
                            subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalAdditionalTokenCost = subs?.ManualTokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    TotalTokenSubscription = new TotalTokenSubscriptionInfo
                    {
                        TotalTokenInMillion = subs?.TokenSubscription?.TotalAdditionalTokenInMillion ??
                                              0.0 + subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalTokenCost = subs?.TokenSubscription?.TotalAdditionalTokenCost ??
                                         0.0 + subs?.ManualTokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    Tags = new string[] { "Is-Valid-PraxisClient" },
                    PaymentMode = "OFFLINE"
                };
                if (subs == null)
                    await _praxisClientSubscriptionService.SaveClientSubscriptionOnOrgCreateUpdate(org.ItemId,
                        orgSubscription, org);
                else
                {
                    subs.NumberOfAuthorizedUsers = org.UserLimit * 2;
                    subs.StorageSubscription = orgSubscription.StorageSubscription;
                    subs.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    subs.TokenSubscription = orgSubscription.TokenSubscription;
                    subs.ManualTokenSubscription = orgSubscription.ManualTokenSubscription;
                    subs.TotalTokenSubscription = orgSubscription.TotalTokenSubscription;
                    subs.TotalPerMonthDueCosts = orgSubscription.TotalPerMonthDueCosts;
                    subs.PaymentMethod = orgSubscription.PaymentMethod;
                    subs.DurationOfSubscription = 12;
                    subs.SubscriptionDate = DateTime.UtcNow;
                    subs.SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1);
                    subs.IsTokenApplied = orgSubscription.IsTokenApplied;
                    subs.OrganizationName = org.ClientName;
                    subs.OrganizationEmail = org.ContactEmail;
                    await _repository.UpdateAsync(c => c.ItemId == subs.ItemId, subs);
                }

                subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(org.ItemId);
                if (subs != null)
                {
                    var not = _repository.GetItem<PraxisClientSubscriptionNotification>(o =>
                        o.OrganizationId == org.ItemId && o.IsActive);
                    if (not == null)
                    {
                        await _praxisClientSubscriptionService.SaveSubscriptionNotification(org.ItemId, subs);
                    }
                    else
                    {
                        not.DurationOfSubscription = subs.DurationOfSubscription.ToString();
                        not.SubscriptionExpirationDate = subs.SubscriptionExpirationDate;
                        await _repository.UpdateAsync(c => c.ItemId == not.ItemId, not);
                    }

                    await _updateClientSubscriptionInformation.ProcessSubscriptionInfoAsync(subs, org.ItemId);
                }
            }

            var depts = _repository.GetItems<PraxisClient>(o =>
                                !o.IsMarkedToDelete && command.OrganizationIds.Contains(o.ParentOrganizationId))
                            ?.ToList() ??
                        new List<PraxisClient>();
            if (command.DepartmentIds?.Count > 0)
            {
                depts = depts.Where(d => command.DepartmentIds.Contains(d.ItemId)).ToList();
            }

            foreach (var dept in depts)
            {
                await _praxisClientSubscriptionService.SaveClientSubscriptionOnClientCreateUpdate(dept.ItemId);
                var subs = await _praxisClientSubscriptionService.GetClientLatestSubscriptionData(dept.ItemId);
                if (subs != null)
                {
                    subs.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    subs.DurationOfSubscription = 12;
                    subs.SubscriptionDate = DateTime.UtcNow;
                    subs.SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1);
                    subs.StorageSubscription = new StorageSubscriptionInfo
                    {
                        IncludedStorageInGigaBites = dept.UserLimit * .5,
                        TotalAdditionalStorageInGigaBites = command.AdditionalStorage ?? 0.0,
                        TotalAdditionalStorageCost = 0.0
                    };
                    subs.TokenSubscription = new TokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = 0,
                        TotalAdditionalTokenInMillion = command.AdditionalToken ?? 0.0,
                        TotalAdditionalTokenCost = 0.0
                    };
                    subs.ManualTokenSubscription = new ManualTokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = 0,
                        TotalAdditionalTokenInMillion = command.AdditionalManualToken ?? 0.0,
                        TotalAdditionalTokenCost = 0.0
                    };
                    subs.TotalTokenSubscription = new TotalTokenSubscriptionInfo
                    {
                        TotalTokenInMillion = command.AdditionalToken ?? 0.0 + command.AdditionalManualToken ?? 0.0,
                        TotalTokenCost = 0.0
                    };
                    subs.IsTokenApplied = command.AdditionalToken > 0;
                    subs.IsManualTokenApplied = command.AdditionalManualToken > 0;
                    await _repository.UpdateAsync(c => c.ItemId == subs.ItemId, subs);

                    var not = _repository.GetItem<PraxisClientSubscriptionNotification>(o =>
                        o.ClientId == dept.ItemId && o.IsActive);
                    if (not == null)
                    {
                        await _praxisClientSubscriptionService.SaveSubscriptionNotificationForClient(dept.ItemId, subs);
                    }
                    else
                    {
                        not.DurationOfSubscription = subs.DurationOfSubscription.ToString();
                        not.SubscriptionExpirationDate = subs.SubscriptionExpirationDate;
                        await _repository.UpdateAsync(c => c.ItemId == not.ItemId, not);
                    }

                    await _updateClientSubscriptionInformation.ProcessSubscriptionInfoAsync(subs,
                        dept.ParentOrganizationId);
                }
            }

            foreach (var dept in depts)
            {
                dept.AuthorizedUserLimit = dept.UserLimit * 2;
                dept.AdditionalStorage = 0;
                dept.AdditionalLanguagesToken = 0;
                dept.AdditionalManualToken = 0;
                await _repository.UpdateAsync(c => c.ItemId == dept.ItemId, dept);
            }

            foreach (var org in orgs)
            {
                org.AuthorizedUserLimit = org.UserLimit * 2;
                org.TotalDepartmentAuthorizedUserLimit = org.TotalDepartmentUserLimit * 2;
                org.TotalDepartmentAdditionalStorageLimit = 0;
                org.TotalDepartmentAdditionalLanguageTokenLimit = 0;
                org.TotalDepartmentAdditionalManualTokenLimit = 0;
                await _repository.UpdateAsync(c => c.ItemId == org.ItemId, org);
            }

        }

        private async Task FixPraxisClientSubscriptionForProd()
        {
            var orgs = _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete)?.ToList() ??
                       new List<PraxisOrganization>();
            foreach (var org in orgs)
            {
                var subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(org.ItemId);
                org.CreateDate = DateTime.UtcNow;
                var orgSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow.ToLocalTime(),
                    DurationOfSubscription = 12,
                    PaymentMethod = subs?.PaymentMethod ?? "Annually",
                    BillingAddress = subs?.BillingAddress,
                    ResponsiblePerson = subs?.ResponsiblePerson,
                    Location = subs?.Location ?? "CH",
                    PerUserCost = subs?.PerUserCost ?? 0,
                    AverageCost = subs?.AverageCost ?? 0,
                    TaxDeduction = subs?.TaxDeduction ?? 0,
                    GrandTotal = subs?.GrandTotal ?? 0,
                    PaymentCurrency = subs?.PaymentCurrency ?? "chf",
                    PaidAmount = subs?.PaidAmount ?? 0,
                    IsTokenApplied = false,
                    IsManualTokenApplied = false,
                    IsActive = true,
                    IsLatest = true,
                    StorageSubscription = new StorageSubscriptionInfo
                    {
                        IncludedStorageInGigaBites = org.UserLimit * .5,
                        TotalAdditionalStorageInGigaBites =
                            subs?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0.0,
                        TotalAdditionalStorageCost = subs?.StorageSubscription?.TotalAdditionalStorageCost ?? 0.0
                    },
                    TokenSubscription = new TokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = subs?.TokenSubscription?.IncludedTokenInMillion ?? 0.0,
                        TotalAdditionalTokenInMillion = subs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalAdditionalTokenCost = subs?.TokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    ManualTokenSubscription = new ManualTokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = subs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0.0,
                        TotalAdditionalTokenInMillion =
                            subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalAdditionalTokenCost = subs?.ManualTokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    TotalTokenSubscription = new TotalTokenSubscriptionInfo
                    {
                        TotalTokenInMillion = subs?.TokenSubscription?.TotalAdditionalTokenInMillion ??
                                              0.0 + subs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0.0,
                        TotalTokenCost = subs?.TokenSubscription?.TotalAdditionalTokenCost ??
                                         0.0 + subs?.ManualTokenSubscription?.TotalAdditionalTokenCost ?? 0.0
                    },
                    Tags = new string[] { "Is-Valid-PraxisClient" },
                    PaymentMode = "OFFLINE"
                };
                if (subs == null)
                    await _praxisClientSubscriptionService.SaveClientSubscriptionOnOrgCreateUpdate(org.ItemId,
                        orgSubscription, org);
                else
                {
                    subs.NumberOfAuthorizedUsers = org.UserLimit * 2;
                    subs.StorageSubscription = orgSubscription.StorageSubscription;
                    subs.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    subs.TokenSubscription = orgSubscription.TokenSubscription;
                    subs.ManualTokenSubscription = orgSubscription.ManualTokenSubscription;
                    subs.TotalTokenSubscription = orgSubscription.TotalTokenSubscription;
                    subs.TotalPerMonthDueCosts = orgSubscription.TotalPerMonthDueCosts;
                    subs.PaymentMethod = orgSubscription.PaymentMethod;
                    subs.DurationOfSubscription = 12;
                    subs.SubscriptionDate = DateTime.UtcNow;
                    subs.SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1);
                    subs.IsTokenApplied = orgSubscription.IsTokenApplied;
                    subs.OrganizationName = org.ClientName;
                    subs.OrganizationEmail = org.ContactEmail;
                    await _repository.UpdateAsync(c => c.ItemId == subs.ItemId, subs);
                }

                subs = await _praxisClientSubscriptionService.GetOrganizationLatestSubscriptionData(org.ItemId);
                if (subs != null)
                {
                    var not = _repository.GetItem<PraxisClientSubscriptionNotification>(o =>
                        o.OrganizationId == org.ItemId && o.IsActive);
                    if (not == null)
                    {
                        await _praxisClientSubscriptionService.SaveSubscriptionNotification(org.ItemId, subs);
                    }
                    else
                    {
                        not.DurationOfSubscription = subs.DurationOfSubscription.ToString();
                        not.SubscriptionExpirationDate = subs.SubscriptionExpirationDate;
                        await _repository.UpdateAsync(c => c.ItemId == not.ItemId, not);
                    }

                    await _updateClientSubscriptionInformation.ProcessSubscriptionInfoAsync(subs, org.ItemId);
                }
            }

            var depts = _repository.GetItems<PraxisClient>(o => !o.IsMarkedToDelete)?.ToList() ??
                        new List<PraxisClient>();
            foreach (var dept in depts)
            {
                var subs = await _praxisClientSubscriptionService.GetClientLatestSubscriptionData(dept.ItemId);
                if (subs != null)
                {
                    subs.SubscritionStatus = nameof(PraxisEnums.ONGOING);
                    subs.DurationOfSubscription = 12;
                    subs.SubscriptionDate = DateTime.UtcNow;
                    subs.SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1);
                    subs.StorageSubscription = new StorageSubscriptionInfo
                    {
                        IncludedStorageInGigaBites = dept.UserLimit * .5,
                        TotalAdditionalStorageInGigaBites = 0.0,
                        TotalAdditionalStorageCost = 0.0
                    };
                    subs.TokenSubscription = new TokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = 0,
                        TotalAdditionalTokenInMillion = 0.0,
                        TotalAdditionalTokenCost = 0.0
                    };
                    subs.ManualTokenSubscription = new ManualTokenSubscriptionInfo
                    {
                        IncludedTokenInMillion = 0,
                        TotalAdditionalTokenInMillion = 0.0,
                        TotalAdditionalTokenCost = 0.0
                    };
                    subs.TotalTokenSubscription = new TotalTokenSubscriptionInfo
                    {
                        TotalTokenInMillion = 0.0,
                        TotalTokenCost = 0.0
                    };
                    subs.IsTokenApplied = false;
                    subs.IsManualTokenApplied = false;
                    await _repository.UpdateAsync(c => c.ItemId == subs.ItemId, subs);

                    var not = _repository.GetItem<PraxisClientSubscriptionNotification>(o =>
                        o.ClientId == dept.ItemId && o.IsActive);
                    if (not == null)
                    {
                        await _praxisClientSubscriptionService.SaveSubscriptionNotificationForClient(dept.ItemId, subs);
                    }
                    else
                    {
                        not.DurationOfSubscription = subs.DurationOfSubscription.ToString();
                        not.SubscriptionExpirationDate = subs.SubscriptionExpirationDate;
                        await _repository.UpdateAsync(c => c.ItemId == not.ItemId, not);
                    }

                    await _updateClientSubscriptionInformation.ProcessSubscriptionInfoAsync(subs,
                        dept.ParentOrganizationId);
                }
            }

            foreach (var dept in depts)
            {
                dept.AuthorizedUserLimit = dept.UserLimit * 2;
                dept.AdditionalStorage = 0;
                dept.AdditionalLanguagesToken = 0;
                dept.AdditionalManualToken = 0;
                await _repository.UpdateAsync(c => c.ItemId == dept.ItemId, dept);
            }

            foreach (var org in orgs)
            {
                org.AuthorizedUserLimit = org.UserLimit * 2;
                org.TotalDepartmentAuthorizedUserLimit = org.TotalDepartmentUserLimit * 2;
                org.TotalDepartmentAdditionalStorageLimit = 0;
                org.TotalDepartmentAdditionalLanguageTokenLimit = 0;
                org.TotalDepartmentAdditionalManualTokenLimit = 0;
                await _repository.UpdateAsync(c => c.ItemId == org.ItemId, org);
            }
        }


        public class LanguageInfo
        {
            public string Language { get; set; }
            public string MotherTongue { get; set; }
        }

        private async Task FixNativeLanguageForPraxisUser()
        {
            var motherTongueList = new List<LanguageInfo>
            {
                new LanguageInfo { Language = "de-DE", MotherTongue = "GERMAN" },
                new LanguageInfo { Language = "fr-FR", MotherTongue = "FRENCH" },
                new LanguageInfo { Language = "it-IT", MotherTongue = "ITALIAN" },
                new LanguageInfo { Language = "en-US", MotherTongue = "ENGLISH" },
                new LanguageInfo { Language = "pt-PT", MotherTongue = "PORTUGUESE" },
                new LanguageInfo { Language = "es-ES", MotherTongue = "SPANISH" },
                new LanguageInfo { Language = "sr-RS", MotherTongue = "SERBIAN" },
                new LanguageInfo { Language = "hr-HR", MotherTongue = "CROATIAN" },
                new LanguageInfo { Language = "sq-AL", MotherTongue = "ALBANIAN" },
                new LanguageInfo { Language = "tr-TR", MotherTongue = "TURKISH" },
                new LanguageInfo { Language = "cs-CZ", MotherTongue = "CZECH" },
                new LanguageInfo { Language = "uk-UA", MotherTongue = "UKRAINIAN" },
                new LanguageInfo { Language = "ru-RU", MotherTongue = "RUSSIAN" },
                new LanguageInfo { Language = "pl-PL", MotherTongue = "POLISH" },
                new LanguageInfo { Language = "ro-RO", MotherTongue = "ROMANIAN" },
                new LanguageInfo { Language = "ta-IN", MotherTongue = "TAMIL" },
                new LanguageInfo { Language = "tl-PH", MotherTongue = "TAGALOG" },
                new LanguageInfo { Language = "hu-HU", MotherTongue = "HUNGARIAN" },
                new LanguageInfo { Language = "bg-BG", MotherTongue = "BULGARIAN" }
            };

            var praxisUsers = _repository.GetItems<PraxisUser>(u => !u.IsMarkedToDelete)?.ToList();

            var tasks = new List<Task>();

            praxisUsers.ForEach(user =>
            {
                var motherTongue = motherTongueList.FirstOrDefault(l =>
                    l.MotherTongue == user.MotherTongue || l.Language == user.MotherTongue);

                if (motherTongue != null)
                {
                    user.MotherTongue = motherTongue.MotherTongue;
                }
                else
                {
                    user.MotherTongue = "GERMAN";
                }

                tasks.Add(_repository.UpdateAsync(a => a.ItemId == user.ItemId, user));
            });

            await Task.WhenAll(tasks);
        }

        private async Task FixClientAdditionalInfoData()
        {
            var praxisClients = _repository
                .GetItems<PraxisClient>(c => !c.IsMarkedToDelete && c.AdditionalInfos != null)?
                .ToList() ?? new List<PraxisClient>();
            var dataNumber = 0;
            var totalCount = praxisClients.Count;
            foreach (var praxisClient in praxisClients)
            {
                praxisClient.AdditionalInfos = praxisClient.AdditionalInfos
                    .Select(a =>
                    {
                        var supplierContactPerson = a.SupplierContactPersons?.ToList()
                                                    ?? new List<PraxisSupplierContactPerson>();
                        if (!string.IsNullOrEmpty(a.ContactPerson))
                        {
                            supplierContactPerson = new List<PraxisSupplierContactPerson>
                            {
                                new()
                                {
                                    ContactId = Guid.NewGuid().ToString(),
                                    Name = a.ContactPerson,
                                    Email = a.Email,
                                    PhoneNumber = a.PhoneNumber,
                                    Position = string.Empty,
                                    IsPrimaryContact = true
                                }
                            };
                        }

                        a.SupplierContactPersons = supplierContactPerson;
                        a.ContactPersons = null;
                        return a;
                    })
                    .ToList();
                await _repository.UpdateAsync(c => c.ItemId == praxisClient.ItemId, praxisClient);
                ++dataNumber;
                Console.WriteLine($"Script Running... update {dataNumber} data out of {totalCount}");
            }
        }

        private async Task FixProcessGuideListingOfEquipment()
        {
            try
            {
                var equipments = _repository
                    .GetItems<PraxisEquipment>(e => !e.IsMarkedToDelete
                                                    && !string.IsNullOrEmpty(e.Topic.Key))?
                    .ToList() ?? new List<PraxisEquipment>();
                var index = 0;
                var totalEquipments = equipments.Count;
                foreach (var equipment in equipments)
                {
                    PraxisForm praxisForm = null;

                    try
                    {
                        praxisForm = JsonConvert.DeserializeObject<PraxisForm>(equipment.Topic.Key);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Deserialization failed: {ex.Message}");
                    }

                    if (praxisForm == null) continue;
                    var processGuideListing = new List<EquipmentProcessGuideListing>
                    {
                        new()
                        {
                            FormTitle = praxisForm.Description,
                            FormId = praxisForm.ItemId,
                        }
                    };
                    var guideList = new List<PraxisKeyValue>
                    {
                        new()
                        {
                            Key = "ProcessGuideListing",
                            Value = JsonConvert.SerializeObject(processGuideListing)
                        }
                    };
                    equipment.MetaValues = guideList;
                    await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalEquipments}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixProcessGuideListingOfEquipment), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in FixProcessGuideListingOfEquipment!!!");
            }
        }

        private async Task FixContactPersonsOfEquipment()
        {
            try
            {
                var equipments = _repository
                    .GetItems<PraxisEquipment>(e => !e.IsMarkedToDelete)?
                    .ToList() ?? new List<PraxisEquipment>();
                var index = 0;
                var totalEquipments = equipments.Count;
                foreach (var equipment in equipments)
                {
                    var contactPersons = new List<PraxisSupplierContactPerson>();
                    if (!string.IsNullOrEmpty(equipment.ContactPerson))
                    {
                        contactPersons.Add(new PraxisSupplierContactPerson
                        {
                            ContactId = Guid.NewGuid().ToString(),
                            Name = equipment.ContactPerson,
                            Email = equipment.Email,
                            PhoneNumber = equipment.PhoneNumber,
                            Position = string.Empty,
                            IsPrimaryContact = true
                        });
                    }

                    equipment.ContactPersons = null;
                    equipment.EquipmentContactsInformation = contactPersons;
                    await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalEquipments}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixContactPersonsOfEquipment), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in FixContactPersonsOfEquipment!!!");
            }
        }

        private async Task UnsetCockpitUnusedFields()
        {
            var collection = _mongoClientRepository.GetCollection(nameof(RiqsTaskCockpitSummary));
            var filter = Builders<BsonDocument>.Filter.Empty;

            var update = Builders<BsonDocument>.Update
                .Unset("DepartmentDetails.$[].LogoFileId")
                .Unset("DepartmentDetails.$[].Email")
                .Unset("DepartmentDetails.$[].InvoiceType")
                .Unset("DepartmentDetails.$[].ReportDispatchType")
                .Unset("DepartmentDetails.$[].EmailType")
                .Unset("DepartmentDetails.$[].ConfigId")
                .Unset("DepartmentDetails.$[].EmailAuthorizationStatus")
                .Unset("DepartmentDetails.$[].ToRecipent")
                .Unset("DepartmentDetails.$[].UIConfiguration")
                .Unset("DepartmentDetails.$[].SLAEmailDispatchType")
                .Unset("DepartmentDetails.$[].SLAEmailCustomReceivers")
                .Unset("DepartmentDetails.$[].Priority")
                .Unset("DepartmentDetails.$[].Language")
                .Unset("DepartmentDetails.$[].CcRecipient")
                .Unset("DepartmentDetails.$[].IsActive")
                .Unset("DepartmentDetails.$[].InputChannel");

            var result = await collection.UpdateManyAsync(filter, update);
            _logger.LogInformation("UnsetCockpitUnusedFields: {ModifiedCount} documents updated.",
                result.ModifiedCount);
            _logger.LogInformation("More info of UnsetCockpitUnusedFields: {Result}", result);
        }

        private async Task SetCockpitDocumentObjectSummaryActiveStatus()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetCockpitDocumentObjectSummaryActiveStatus));
            try
            {
                var summaries = _repository.GetItems<CockpitObjectArtifactSummary>()?.ToList() ??
                                new List<CockpitObjectArtifactSummary>();
                var totalSummaries = summaries.Count;
                var index = 0;
                _logger.LogInformation("Total CockpitObjectArtifactSummary found: {TotalSummaries}", totalSummaries);
                var statusKey =
                    LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];
                var fileStatusActive = ((int)LibraryFileStatusEnum.ACTIVE).ToString();

                foreach (var summary in summaries)
                {
                    var artifact =
                        await _repository.GetItemAsync<ObjectArtifact>(a => a.ItemId == summary.ObjectArtifactId);
                    if (artifact == null) continue;
                    artifact.MetaData ??= new Dictionary<string, MetaValuePair>();

                    if (artifact.MetaData.TryGetValue(statusKey, out MetaValuePair currentMetaDataStatusValue))
                    {
                        summary.IsActive = currentMetaDataStatusValue.Value == fileStatusActive;
                    }
                    else
                    {
                        summary.IsActive = true;
                    }

                    await _repository.UpdateAsync(s => s.ItemId == summary.ItemId, summary);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalSummaries}");
                }

                _logger.LogInformation("CockpitObjectArtifactSummary Active Status updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(SetCockpitDocumentObjectSummaryActiveStatus), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetCockpitDocumentObjectSummaryActiveStatus!!!");
            }
        }

        private async Task SetCompanyContactInfoInEquipmentDetails()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetCompanyContactInfoInEquipmentDetails));
            try
            {
                var equipments = _repository.GetItems<PraxisEquipment>()?.ToList() ?? new List<PraxisEquipment>();
                var totalEquipments = equipments.Count;
                var index = 0;
                _logger.LogInformation("Total PraxisEquipment found: {TotalEquipments}", totalEquipments);

                foreach (var equipment in equipments)
                {
                    var companyContactInfo = new List<EquipmentCompanyContactInfo>();
                    foreach (var contact in equipment.EquipmentContactsInformation ??
                                            new List<PraxisSupplierContactPerson>())
                    {
                        try
                        {
                            var client = _repository.GetItem<PraxisClient>(pc =>
                                pc.AdditionalInfos != null &&
                                pc.AdditionalInfos.Any(pca =>
                                    pca.SupplierContactPersons != null &&
                                    pca.SupplierContactPersons.Any(s => s.ContactId == contact.ContactId)));
                            var companyInfo = client?.AdditionalInfos.FirstOrDefault(a =>
                                a.SupplierContactPersons.Any(s => s.ContactId == contact.ContactId));
                            Console.WriteLine($"Company Info: {JsonConvert.SerializeObject(companyInfo)}");
                            if (companyInfo == null) continue;
                            companyContactInfo.Add(new EquipmentCompanyContactInfo
                            {
                                CompanyId = companyInfo.ItemId,
                                CompanyName = companyInfo.Name,
                                ContactId = contact.ContactId,
                                Name = contact.Name,
                                Email = contact.Email,
                                PhoneNumber = contact.PhoneNumber,
                                Position = contact.Position,
                                IsPrimaryContact = contact.IsPrimaryContact

                            });
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(
                                "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                                nameof(SetCompanyContactInfoInEquipmentDetails), e.Message, e.StackTrace);
                            Console.WriteLine("Error occured in Preparing company contact info!!!");
                        }
                    }

                    var companyContactKeyValuePair = new PraxisKeyValue
                    {
                        Key = "CompanyContactInfo",
                        Value = JsonConvert.SerializeObject(companyContactInfo)
                    };
                    var metaValues = equipment.MetaValues?.ToList() ?? new List<PraxisKeyValue>();
                    if (metaValues.Exists(k => k.Key == "CompanyContactInfo"))
                    {
                        metaValues.RemoveAll(key => key.Key == "CompanyContactInfo");
                    }

                    metaValues.Add(companyContactKeyValuePair);
                    equipment.MetaValues = metaValues;
                    await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalEquipments}");
                }

                _logger.LogInformation("Company Contact Info in Equipment Details updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(SetCompanyContactInfoInEquipmentDetails), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetCompanyContactInfoInEquipmentDetails!!!");
            }
        }

        private async Task SetClientIdInDmsArtifactReference()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetClientIdInDmsArtifactReference));
            try
            {
                var dmsArtifactReferences = _repository.GetItems<DmsArtifactUsageReference>()?.ToList() ??
                                            new List<DmsArtifactUsageReference>();
                var totalDmsArtifactReferences = dmsArtifactReferences.Count;
                var index = 0;
                _logger.LogInformation("Total DmsArtifactUsageReference found: {TotalDmsArtifactReferences}",
                    totalDmsArtifactReferences);

                foreach (var dmsArtifactReference in dmsArtifactReferences)
                {
                    await UpdateClient(dmsArtifactReference);
                    await _repository.UpdateAsync(d => d.ItemId == dmsArtifactReference.ItemId, dmsArtifactReference);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalDmsArtifactReferences}");
                }

                _logger.LogInformation("Client Id in DmsArtifactReference updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(SetClientIdInDmsArtifactReference), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetClientIdInDmsArtifactReference!!!");
            }
        }

        private async Task SetDueDateInCockpitProcessGuide()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetDueDateInCockpitProcessGuide));
            try
            {
                var summary = _repository.GetItems<RiqsTaskCockpitSummary>(s =>
                    s.RelatedEntityName == CockpitTypeNameEnum.PraxisProcessGuide &&
                    !s.IsMarkedToDelete)?.ToList() ?? new List<RiqsTaskCockpitSummary>();
                var totalProcessGuides = summary.Count;
                var index = 0;
                _logger.LogInformation("Total PraxisProcessGuide found: {TotalProcessGuides}", totalProcessGuides);

                foreach (var s in summary)
                {
                    s.AdditionalInfo ??= new Dictionary<string, object>();
                    var groupName = (string)s.AdditionalInfo["Group"];
                    if (!s.AdditionalInfo.TryAdd("ShowDueDate", groupName == nameof(PraxisProcessGuide))) continue;
                    await _repository.UpdateAsync(r => r.ItemId == s.ItemId, s);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalProcessGuides}");
                }

                _logger.LogInformation("Due Date in Cockpit Process Guide updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(SetDueDateInCockpitProcessGuide), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetDueDateInCockpitProcessGuide!!!");
            }
        }

        private async Task UpdateClient(DmsArtifactUsageReference reference)
        {
            try
            {
                var itemId = reference.RelatedEntityId;
                var relatedEntityName = reference.RelatedEntityName;
                ArgumentNullException.ThrowIfNull(itemId);
                ArgumentNullException.ThrowIfNull(relatedEntityName);
                if (relatedEntityName == nameof(PraxisForm))
                {
                    var data = await _repository.GetItemAsync<PraxisForm>(f => f.ItemId == itemId);
                    var c = data?.ClientInfos?.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(c?.ClientId))
                    {
                        reference.ClientInfos = data.ClientInfos.ToList();
                    }

                    reference.OrganizationIds = data?.OrganizationIds;
                }
                else if (relatedEntityName == nameof(PraxisOpenItem))
                {
                    var data = await _repository.GetItemAsync<PraxisOpenItem>(f => f.ItemId == itemId);
                    reference.ClientInfos = GetClientInfo(data?.ClientId ?? string.Empty);
                }
                else if (relatedEntityName == nameof(PraxisEquipment))
                {
                    var data = await _repository.GetItemAsync<PraxisEquipment>(f => f.ItemId == itemId);
                    if (data?.ClientId != null)
                    {
                        reference.ClientInfos = new List<FormSpecificClientInfo>
                        {
                            new()
                            {
                                ClientName = data.ClientName,
                                ClientId = data.ClientId
                            }
                        };
                    }
                }
                else if (relatedEntityName == nameof(PraxisEquipmentMaintenance))
                {
                    var data = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(f => f.ItemId == itemId);
                    reference.ClientInfos = GetClientInfo(data?.ClientId ?? string.Empty);
                }
                else if (relatedEntityName == nameof(CirsGenericReport))
                {
                    var data = await _repository.GetItemAsync<CirsGenericReport>(f => f.ItemId == itemId);
                    reference.ClientInfos =
                        GetClientInfo(data?.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty);
                    reference.OrganizationId = data?.OrganizationId;
                }
                else if (relatedEntityName == nameof(RiqsShiftPlan))
                {
                    var data = await _repository.GetItemAsync<RiqsShiftPlan>(f => f.ItemId == itemId);
                    reference.ClientInfos = GetClientInfo(data?.Shift?.DepartmentId ?? string.Empty);
                    reference.OrganizationId = data?.Shift?.OrganizationId;
                }

                if (reference.MetaData.ContainsKey("ClientId"))
                {
                    reference.MetaData.Remove("ClientId");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(GetClientInfo), e.Message, e.StackTrace);
                Console.WriteLine($"Error occured in GetClientId!!! Message: {e.Message}");
            }
        }

        private List<FormSpecificClientInfo> GetClientInfo(string clientId)
        {
            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId);
            if (client == null) return null;
            return new List<FormSpecificClientInfo>
            {
                new()
                {
                    ClientId = client.ItemId,
                    ClientName = client.ClientName
                }
            };
        }

        private async Task SetClientIdInCockpitDocumentSummary()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetClientIdInCockpitDocumentSummary));
            try
            {
                var summaries = _repository
                    .GetItems<CockpitObjectArtifactSummary>(s =>
                        !s.IsMarkedToDelete)?.ToList() ?? new List<CockpitObjectArtifactSummary>();
                var totalSummaries = summaries.Count;
                var index = 0;
                _logger.LogInformation("Total cockpit document summary found: {TotalSummaries}", totalSummaries);
                var shiftPlanKey = nameof(RiqsShiftPlan);
                foreach (var s in summaries)
                {
                    if (s.AdditionalInfos?.TryGetValue("RelatedEntityName", out var value) == true)
                    {
                        if (shiftPlanKey.Equals(value))
                        {
                            var shiftPlanId = s.AdditionalInfos["RelatedEntityId"]?.ToString();
                            var shiftPlan = await _repository.GetItemAsync<RiqsShiftPlan>(r => r.ItemId == shiftPlanId);
                            s.AdditionalInfos["ClientId"] = shiftPlan?.Shift?.DepartmentId;
                            s.AdditionalInfos["RelatedEntityName"] = nameof(RiqsShiftPlan);
                            s.AdditionalInfos["RelatedEntityId"] = shiftPlanId;
                        }
                        else
                        {
                            var objectArtifact =
                                _repository.GetItem<ObjectArtifact>(o => o.ItemId == s.ObjectArtifactId);
                            if (objectArtifact?.MetaData?.TryGetValue("DepartmentId", out var client) == true)
                            {
                                s.AdditionalInfos["ClientId"] = client.Value;
                            }

                            s.AdditionalInfos["RelatedEntityName"] = nameof(ObjectArtifact);
                            s.AdditionalInfos["RelatedEntityId"] = s.ObjectArtifactId;
                        }
                    }
                    else
                    {
                        var objectArtifact =
                            _repository.GetItem<ObjectArtifact>(o => o.ItemId == s.ObjectArtifactId);
                        s.AdditionalInfos ??= new Dictionary<string, object>();
                        if (objectArtifact?.MetaData?.TryGetValue("DepartmentId", out var client) == true)
                        {
                            s.AdditionalInfos["ClientId"] = client.Value;
                        }

                        s.AdditionalInfos["RelatedEntityName"] = nameof(ObjectArtifact);
                        s.AdditionalInfos["RelatedEntityId"] = s.ObjectArtifactId;
                    }

                    await _repository.UpdateAsync(r => r.ItemId == s.ItemId, s);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalSummaries}");
                }

                _logger.LogInformation("Client Id in Cockpit Document Summary updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(SetClientIdInCockpitDocumentSummary), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetClientIdInCockpitDocumentSummary!!!");
            }
        }

        private async Task SetClientIdInReportingCockpitSummaryDependentTask()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(SetClientIdInReportingCockpitSummaryDependentTask));
            try
            {
                var summaries = _repository
                    .GetItems<RiqsTaskCockpitSummary>(s =>
                        !s.IsMarkedToDelete && s.RelatedEntityName == CockpitTypeNameEnum.CirsGenericReport)?
                    .ToList() ?? new List<RiqsTaskCockpitSummary>();
                var totalSummaries = summaries.Count;
                var index = 0;
                _logger.LogInformation("Total ReportingCockpitSummaryDependentTask found: {TotalSummaries}",
                    totalSummaries);
                foreach (var s in summaries)
                {
                    var taskIds = s.DependentTasks?.Select(f => f.TaskId).ToList() ?? new List<string>();
                    foreach (var taskId in taskIds)
                    {
                        var task = s.DependentTasks?.FirstOrDefault(d => d.TaskId == taskId) ??
                                   new PraxisQueuedDependentTask();
                        s.DependentTasks = s.DependentTasks.Where(d => d.TaskId != task.TaskId).ToList();
                        _reportingTaskCockpitSummaryCommandService.OnDependentTaskModifyUpdateSummary(s);

                        if (task.TaskType == nameof(PraxisOpenItem))
                        {
                            var openItem = await _repository.GetItemAsync<PraxisOpenItem>(o => o.ItemId == task.TaskId);
                            var assignedTo = GetOpenItemAssignedMembers(openItem);
                            var rolesAllowedToRead = !assignedTo.Any()
                                ? GetGeneralDepartmentLevelRoles(openItem.ClientId)
                                : new List<string>();
                            var newTask = task;
                            newTask.AssignedTo = assignedTo.Except(task.ResponseSubmittedBy).ToList();
                            newTask.RolesAllowedToRead = rolesAllowedToRead;
                            newTask.ClientId = openItem.ClientId;
                            s.DependentTasks.Add(newTask);
                            _reportingTaskCockpitSummaryCommandService.OnDependentTaskModifyUpdateSummary(s);
                        }
                        else if (task.TaskType == nameof(PraxisProcessGuide))
                        {
                            var processGuide = await _repository.GetItemAsync<PraxisProcessGuide>(p =>
                                p.ItemId == task.TaskId);
                            var clients = processGuide.Clients;
                            foreach (var client in clients)
                            {
                                var newTask = task;
                                var assignedTo = client.HasSpecificControlledMembers
                                    ? client.ControlledMembers.ToList()
                                    : new List<string>();
                                var rolesAllowedToRead = !client.HasSpecificControlledMembers
                                    ? GetGeneralDepartmentLevelRoles(client.ClientId)
                                    : new List<string>();
                                newTask.AssignedTo = assignedTo.Except(task.ResponseSubmittedBy).ToList();
                                newTask.RolesAllowedToRead = rolesAllowedToRead;
                                newTask.ClientId = client.ClientId;
                                s.DependentTasks.Add(newTask);
                                _reportingTaskCockpitSummaryCommandService.OnDependentTaskModifyUpdateSummary(s);
                            }
                        }
                    }

                    s.LastUpdateDate = DateTime.UtcNow;
                    await _repository.UpdateAsync(r => r.ItemId == s.ItemId, s);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalSummaries}");
                }

                _logger.LogInformation("Client Id in ReportingCockpitSummaryDependentTask updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(SetClientIdInReportingCockpitSummaryDependentTask), e.Message, e.StackTrace);
                Console.WriteLine("Error occured in SetClientIdInReportingCockpitSummaryDependentTask!!!");
            }
        }

        private List<string> GetOpenItemAssignedMembers(PraxisOpenItem openItem)
        {
            var assignedMembers = openItem.ControlledMembers?.ToList() ?? new List<string>();
            if (openItem.ControlledGroups?.Any() ?? false)
            {
                var groupMembers = _repository.GetItems<PraxisUser>(pu =>
                        !pu.IsMarkedToDelete &&
                        pu.Active &&
                        pu.ClientList != null &&
                        pu.ClientList.Any(c =>
                            c.ClientId == openItem.ClientId &&
                            c.Roles != null &&
                            c.Roles.Any(r => openItem.ControlledGroups.Contains(r))))?
                    .Select(g => g.ItemId)
                    .ToList() ?? new List<string>();
                assignedMembers.AddRange(groupMembers);
            }

            return assignedMembers.Distinct().ToList();
        }

        private List<string> GetGeneralDepartmentLevelRoles(string clientId)
        {
            return new List<string>
            {
                $"{RoleNames.PowerUser_Dynamic}_{clientId}",
                $"{RoleNames.Leitung_Dynamic}_{clientId}",
                $"{RoleNames.MpaGroup_Dynamic}_{clientId}",
            };
        }

        private async Task MarkToDeleteCockpitData()
        {
            try
            {
                var cockpitDocumentActivityMetricss =
                    _repository.GetItems<CockpitDocumentActivityMetrics>(p => !p.IsMarkedToDelete)?.ToList();

                if (cockpitDocumentActivityMetricss != null)
                {
                    foreach (var item in cockpitDocumentActivityMetricss)
                    {

                        item.IsMarkedToDelete = true;
                        item.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                        item.LastUpdatedBy = _securityContextProvider.GetSecurityContext().UserId;

                        await _repository.UpdateAsync(s => s.ItemId == item.ItemId, item);
                    }
                }

                var cockpitObjectArtifactSummarys =
                    _repository.GetItems<CockpitObjectArtifactSummary>(p => !p.IsMarkedToDelete)?.ToList();

                if (cockpitObjectArtifactSummarys != null)
                {
                    foreach (var item in cockpitObjectArtifactSummarys)
                    {

                        item.IsMarkedToDelete = true;
                        item.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                        item.LastUpdatedBy = _securityContextProvider.GetSecurityContext().UserId;

                        await _repository.UpdateAsync(s => s.ItemId == item.ItemId, item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(MarkToDeleteCockpitData), ex.Message, ex.StackTrace);
            }
        }

        private async Task SubscriptionUpdateForSystemAdmin(ResolveProdDataIssuesCommand command)
        {
            try
            {
                var organization = _repository.GetItem<PraxisOrganization>(o =>
                    o.ItemId == PraxisConstants.RQMonitorClientId && !o.IsMarkedToDelete);
                if (organization != null)
                {
                    var organizationSubsPayload = new OrganizationSubscription
                    {
                        OrganizationId = organization.ItemId,
                        TotalTokenSize = command.Payload.AdditionalToken ?? 0.0,
                        TotalStorageSize = 5.0 + (command.Payload.AdditionalStorage ?? 0.0),
                        TokenOfOrganization = command.Payload.AdditionalToken ?? 0.0,
                        StorageOfOrganization = 5.0,
                        TokenOfUnits = command.Payload.AdditionalToken ?? 0.0,
                        StorageOfUnits = command.Payload.AdditionalStorage ?? 0.0,
                        SubscriptionDate = DateTime.UtcNow,
                        SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1),
                        IsTokenApplied = true,
                        TotalManualTokenSize = command.Payload.AdditionalManualToken ?? 0.0,
                        ManualTokenOfOrganization = command.Payload.AdditionalManualToken ?? 0.0,
                        ManualTokenOfUnits = command.Payload.AdditionalManualToken ?? 0.0,
                        IsManualTokenApplied = true
                    };

                    await _organizationSubscriptionService.SaveOrganizationSubscription(organizationSubsPayload);

                }

                var dept = _repository.GetItem<PraxisClient>(o =>
                    o.ParentOrganizationId == PraxisConstants.RQMonitorClientId && !o.IsMarkedToDelete);
                if (dept != null)
                {
                    var newDeptSubscription = new PraxisClientSubscription
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        ClientId = dept.ItemId,
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        DurationOfSubscription = 12,
                        SubscriptionDate = DateTime.UtcNow,
                        SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(12).AddSeconds(-1),
                        IsTokenApplied = false,
                        IsManualTokenApplied = false,
                        IsActive = true,
                        IsLatest = true,
                        StorageSubscription = new StorageSubscriptionInfo
                        {
                            IncludedStorageInGigaBites = 5.0,
                            TotalAdditionalStorageInGigaBites = command.Payload.AdditionalStorage ?? 0.0,
                            TotalAdditionalStorageCost = 0.0
                        },
                        TokenSubscription = new TokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = 5.0,
                            TotalAdditionalTokenInMillion = command.Payload.AdditionalToken ?? 0.0,
                            TotalAdditionalTokenCost = 0.0
                        },
                        ManualTokenSubscription = new ManualTokenSubscriptionInfo
                        {
                            IncludedTokenInMillion = 5.0,
                            TotalAdditionalTokenInMillion = command.Payload.AdditionalManualToken ?? 0.0,
                            TotalAdditionalTokenCost = 0.0
                        },
                        TotalTokenSubscription = new TotalTokenSubscriptionInfo
                        {
                            TotalTokenInMillion = command.Payload.AdditionalToken ??
                                                  0.0 + command.Payload.AdditionalManualToken ?? 0.0,
                            TotalTokenCost = 0.0
                        },
                        Tags = new string[] { "Is-Valid-PraxisClient" },
                        PaymentMode = "OFFLINE"
                    };

                    await _departmentSubscriptionService.SaveDepartmentSubscription(dept.ItemId, newDeptSubscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(SubscriptionUpdateForSystemAdmin), ex.Message, ex.StackTrace);
            }
        }

        private async Task DeleteAllDataForRiqsAIConversation()
        {
            try
            {
                var collection = _ecapMongoDbDataContextProvider
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("RiqsAIConversations");

                var filter = Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Eq("RelatedEntityId", BsonNull.Value),
                    Builders<BsonDocument>.Filter.Eq("RelatedEntityName", BsonNull.Value)
                );

                await collection.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(DeleteAllDataForRiqsAIConversation), ex.Message, ex.StackTrace);
            }
        }

        private async Task FixRiqsPediaDeletedUsers(ResolveProdDataIssuesPayload payload)
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixRiqsPediaDeletedUsers));
            try
            {
                var objectArtifacts = _repository.GetItems<ObjectArtifact>(o =>
                    !o.IsMarkedToDelete && payload.OrganizationIds.Contains(o.OrganizationId)).ToList();

                foreach (var objectArtifact in objectArtifacts)
                {
                    var praxisUser =
                        await _repository.GetItemAsync<PraxisUser>(o => o.UserId == objectArtifact.OwnerId);
                    if (praxisUser == null)
                    {
                        objectArtifact.CreatedBy = payload.UserId;
                        objectArtifact.OwnerId = payload.UserId;
                        await _repository.UpdateAsync(o => o.ItemId == objectArtifact.ItemId, objectArtifact);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixRiqsPediaDeletedUsers), e.Message, e.StackTrace);
            }
        }

        private async Task RemoveDeletedRiqsPediaFilesFromEverywhere()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(RemoveDeletedRiqsPediaFilesFromEverywhere));
            try
            {
                await ReviseFilesFromDayPlanning();
                await ReviseFilesFromDayPlanner();
                await ReviseFilesFromOpenItem();
                await ReviseFilesFromPraxisForms();
                await ReviseFilesFromCirsGenericReport();
                await ReviseFilesFromEquipments();
                await ReviseFilesFromEquipmentMaintenance();

            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(RemoveDeletedRiqsPediaFilesFromEverywhere), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromDayPlanning()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromDayPlanning));
            try
            {
                var currentDate = DateTime.UtcNow.Date;
                var activeShiftPlan = _repository.GetItems<RiqsShiftPlan>(s =>
                    !s.IsMarkedToDelete &&
                    s.ShiftDate >= currentDate &&
                    s.Shift != null);
                var index = 0;
                var totalShiftPlan = activeShiftPlan.Count();
                foreach (var riqsShiftPlan in activeShiftPlan)
                {
                    var isNoFiles = (riqsShiftPlan.Shift.Files == null || riqsShiftPlan.Shift.Files.Count == 0) &&
                                    (riqsShiftPlan.Shift.LibraryForms == null || riqsShiftPlan.Shift.LibraryFormResponses.Count == 0);
                    if (isNoFiles)
                    {
                        ++index;
                        continue;
                    }
                    await ExtractActiveFiles(riqsShiftPlan.Shift.Files);
                    await ExtractActiveFiles(riqsShiftPlan.Shift.LibraryForms);
                    await _repository.UpdateAsync(sp => sp.ItemId == riqsShiftPlan.ItemId, riqsShiftPlan);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalShiftPlan}");
                }

                _logger.LogInformation("Files from Day Planning updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromDayPlanning), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromDayPlanner()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromDayPlanner));
            try
            {
                var activeDayPlanner = _repository.GetItems<RiqsShift>(s =>
                    !s.IsMarkedToDelete);
                var index = 0;
                var totalDayPlanner = activeDayPlanner.Count();
                foreach (var riqsDayPlanner in activeDayPlanner)
                {
                    var isNoFiles = (riqsDayPlanner.Files == null || riqsDayPlanner.Files.Count == 0) &&
                                    (riqsDayPlanner.LibraryForms == null || riqsDayPlanner.LibraryForms.Count == 0);
                    if (isNoFiles)
                    {
                        ++index;
                        continue;
                    }
                    await ExtractActiveFiles(riqsDayPlanner.Files);
                    await ExtractActiveFiles(riqsDayPlanner.LibraryForms);
                    await _repository.UpdateAsync(sp => sp.ItemId == riqsDayPlanner.ItemId, riqsDayPlanner);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalDayPlanner}");
                }

                _logger.LogInformation("Files from Day Planner updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromDayPlanner), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromOpenItem()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromOpenItem));
            try
            {
                var activeOpenItems = _repository.GetItems<PraxisOpenItem>(s =>
                    !s.IsMarkedToDelete);
                var index = 0;
                var totalOpenItems = activeOpenItems.Count();
                foreach (var openItem in activeOpenItems)
                {
                    var isNoFiles = openItem.DocumentInfo == null || !openItem.DocumentInfo.Any();
                    if (isNoFiles)
                    {
                        ++index;
                        continue;
                    }
                    var activeDocuments = new List<PraxisOpenItemDocument>();
                    foreach (var doc in openItem.DocumentInfo)
                    {
                        if (await IsActiveDocument(doc.DocumentId))
                        {
                            activeDocuments.Add(doc);
                        }
                    }
                    openItem.DocumentInfo = activeDocuments;
                    await _repository.UpdateAsync(sp => sp.ItemId == openItem.ItemId, openItem);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalOpenItems}");
                }

                _logger.LogInformation("Files from Open Item updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromOpenItem), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromPraxisForms()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromPraxisForms));
            try
            {
                var activeForms = _repository.GetItems<PraxisForm>(s =>
                    !s.IsMarkedToDelete);
                var index = 0;
                var totalForms = activeForms.Count();
                foreach (var form in activeForms)
                {
                    var allOtherFileIds = new List<string>();
                    allOtherFileIds.AddRange(form.ProcessGuideCheckList?
                        .Where(p => p.ProcessGuideTask != null)
                        .SelectMany(p => p.ProcessGuideTask ?? Enumerable.Empty<ProcessGuideTask>())
                        .Where(p => p.Files != null)
                        .SelectMany(p => p.Files ?? Enumerable.Empty<PraxisDocument>())
                        .Where(f => f.DocumentId != null)
                        .Select(f => f.DocumentId) ?? Enumerable.Empty<string>());
                    allOtherFileIds.AddRange(form.QuestionsList?
                        .Where(q => q.Files != null)
                        .SelectMany(q => q.Files ?? Enumerable.Empty<PraxisDocument>())
                        .Where(f => f.DocumentId != null)
                        .Select(f => f.DocumentId) ?? Enumerable.Empty<string>());
                    if (allOtherFileIds.Count == 0 && !(form.Files?.Count() > 0))
                    {
                        ++index;
                        continue;
                    }
                    var activeFiles = new List<string>();
                    foreach (var fileId in allOtherFileIds)
                    {
                        if (await IsActiveDocument(fileId))
                        {
                            activeFiles.Add(fileId);
                        }
                    }

                    foreach (var checkList in form.ProcessGuideCheckList ?? Enumerable.Empty<ClientSpecificCheckList>())
                    {
                        foreach (var processGuideTask in checkList.ProcessGuideTask)
                        {
                            processGuideTask.Files = processGuideTask.Files
                                .Where(f => activeFiles.Contains(f.DocumentId));
                        }
                    }

                    foreach (var praxisQuestion in form.QuestionsList ?? Enumerable.Empty<PraxisQuestion>())
                    {
                        praxisQuestion.Files = praxisQuestion.Files
                            .Where(f => activeFiles.Contains(f.DocumentId));
                    }
                    await ExtractActiveFiles(form.Files?.ToList());
                    await _repository.UpdateAsync(sp => sp.ItemId == form.ItemId, form);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalForms}");
                }

                _logger.LogInformation("Files from Praxis Forms updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromPraxisForms), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromCirsGenericReport()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromCirsGenericReport));
            try
            {
                var activeReports = _repository.GetItems<CirsGenericReport>(s =>
                    !s.IsMarkedToDelete);
                var index = 0;
                var totalReports = activeReports.Count();
                foreach (var report in activeReports)
                {
                    var allAttachedFileIds = new List<string>();
                    allAttachedFileIds.AddRange(report.AttachedDocuments?
                        .Select(p => p.ItemId) ?? Enumerable.Empty<string>());
                    allAttachedFileIds.Add(report.AttachedForm?.ItemId ?? string.Empty);

                    if (allAttachedFileIds.Count == 0)
                    {
                        ++index;
                        continue;
                    }

                    var activeFileIds = new List<string>();
                    foreach (var fileId in allAttachedFileIds)
                    {
                        if (await IsActiveDocument(fileId))
                        {
                            activeFileIds.Add(fileId);
                        }
                    }

                    report.AttachedDocuments = report.AttachedDocuments?
                        .Where(f => activeFileIds.Contains(f.ItemId))
                        .ToList();
                    if (report.AttachedForm == null || !activeFileIds.Contains(report.AttachedForm.ItemId))
                    {
                        report.AttachedForm = null;
                    }

                    await _repository.UpdateAsync(sp => sp.ItemId == report.ItemId, report);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalReports}");
                }

                _logger.LogInformation("Files from Cirs Generic Report updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromCirsGenericReport), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromEquipments()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromEquipments));
            try
            {
                var activeEquipments = _repository.GetItems<PraxisEquipment>(s =>
                    !s.IsMarkedToDelete);
                var index = 0;
                var totalEquipments = activeEquipments.Count();
                foreach (var equipment in activeEquipments)
                {
                    if (!(equipment.Files?.Count() > 0))
                    {
                        ++index;
                        continue;
                    }
                    await ExtractActiveFiles(equipment.Files?.ToList());
                    await _repository.UpdateAsync(sp => sp.ItemId == equipment.ItemId, equipment);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalEquipments}");
                }

                _logger.LogInformation("Files from Equipments updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromEquipments), e.Message, e.StackTrace);
            }
        }

        private async Task ReviseFilesFromEquipmentMaintenance()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(ReviseFilesFromEquipmentMaintenance));
            try
            {
                var activeEquipments = _repository.GetItems<PraxisEquipmentMaintenance>(s =>
                    !s.IsMarkedToDelete &&
                    !(s.CompletionStatus != null &&
                     s.CompletionStatus.Value == "DONE"));
                var index = 0;
                var totalEquipments = activeEquipments.Count();
                foreach (var equipment in activeEquipments)
                {
                    if (!(equipment.LibraryForms?.Count > 0))
                    {
                        ++index;
                        continue;
                    }
                    await ExtractActiveFiles(equipment.LibraryForms);
                    await _repository.UpdateAsync(sp => sp.ItemId == equipment.ItemId, equipment);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalEquipments}");
                }

                _logger.LogInformation("Files from Equipment Maintenance updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(ReviseFilesFromEquipmentMaintenance), e.Message, e.StackTrace);
            }
        }

        private async Task ExtractActiveFiles(List<PraxisDocument> files)
        {
            var activeFiles = new List<PraxisDocument>();
            if (files == null) return;

            foreach (var file in files)
            {
                if (await IsActiveDocument(file.DocumentId))
                {
                    activeFiles.Add(file);
                }
            }

            files.Clear();
            files.AddRange(activeFiles);
        }

        private async Task ExtractActiveFiles(List<PraxisLibraryEntityDetail> files)
        {
            var activeFiles = new List<PraxisLibraryEntityDetail>();
            if (files == null) return;

            foreach (var file in files)
            {
                if (await IsActiveDocument(file.LibraryFormId))
                {
                    activeFiles.Add(file);
                }
            }

            files.Clear();
            files.AddRange(activeFiles);
        }

        private async Task<bool> IsActiveDocument(string objectArtifactId)
        {
            var statusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];
            var active = ((int)LibraryBooleanEnum.TRUE).ToString();
            var artifact = await _repository.GetItemAsync<ObjectArtifact>(a => a.ItemId == objectArtifactId);
            return artifact is { IsMarkedToDelete: false } &&
                   _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, statusKey) == active;
        }

        private async Task RemoveCockpitDocumentTaskOfStandardPrinciple()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(RemoveCockpitDocumentTaskOfStandardPrinciple));
            try
            {
                var activityKey = $"{CockpitDocumentActivityEnum.DOCUMENTS_TO_APPROVE}";
                var cockpitDocumentTasks = _repository.GetItems<CockpitDocumentActivityMetrics>(s =>
                    !s.IsMarkedToDelete && s.ActivityKey == activityKey);
                var index = 0;
                var totalCockpitDocumentTasks = cockpitDocumentTasks.Count();
                foreach (var cockpitDocumentTask in cockpitDocumentTasks)
                {
                    await FilterOutStandardPrincipleFiles(cockpitDocumentTask);
                    await _repository.UpdateAsync(sp => sp.ItemId == cockpitDocumentTask.ItemId, cockpitDocumentTask);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalCockpitDocumentTasks}");
                }

                _logger.LogInformation("Cockpit Document Task of Standard Principle removed successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(RemoveCockpitDocumentTaskOfStandardPrinciple), e.Message, e.StackTrace);
            }
        }

        private Task FilterOutStandardPrincipleFiles(CockpitDocumentActivityMetrics cockpitDocumentTask)
        {
            if (cockpitDocumentTask.CockpitObjectArtifactSummaryIds == null ||
                !cockpitDocumentTask.CockpitObjectArtifactSummaryIds.Any())
            {
                return Task.CompletedTask;
            }

            var summaryIdSet = new HashSet<string>(cockpitDocumentTask.CockpitObjectArtifactSummaryIds);

            var artifactSummaries = _repository.GetItems<CockpitObjectArtifactSummary>(c =>
                    summaryIdSet.Contains(c.ItemId) &&
                    !c.IsMarkedToDelete &&
                    c.IsActive)
                .ToList();

            if (!artifactSummaries.Any())
            {
                cockpitDocumentTask.CockpitObjectArtifactSummaryIds = Array.Empty<string>();
                return Task.CompletedTask;
            }

            var artifactIdSet = new HashSet<string>(artifactSummaries.Select(a => a.ObjectArtifactId));
            var artifacts = _repository.GetItems<ObjectArtifact>(o =>
                    artifactIdSet.Contains(o.ItemId) &&
                    !o.IsMarkedToDelete)
                .ToList();

            var activeArtifactSummaryIds = new List<string>();

            foreach (var summary in artifactSummaries)
            {
                var artifact = artifacts.FirstOrDefault(a => a.ItemId == summary.ObjectArtifactId);
                if (artifact == null) continue;

                var controlMechanism = _objectArtifactUtilityService
                    .GetOrganizationLibraryControlMechanism(artifact.OrganizationId)
                    .ControlMechanismName;

                if (controlMechanism != LibraryControlMechanismConstant.Standard)
                {
                    activeArtifactSummaryIds.Add(summary.ItemId);
                }
            }

            cockpitDocumentTask.CockpitObjectArtifactSummaryIds = activeArtifactSummaryIds.ToArray();
            return Task.CompletedTask;
        }

        private async Task FixTaskCreatorInReportingCockpitSummary()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixTaskCreatorInReportingCockpitSummary));
            try
            {
                var summaries = _repository.GetItems<RiqsTaskCockpitSummary>(s =>
                    !s.IsMarkedToDelete && s.RelatedEntityName == CockpitTypeNameEnum.CirsGenericReport)?
                    .ToList() ?? new List<RiqsTaskCockpitSummary>();
                var index = 0;
                var totalSummaries = summaries.Count;
                foreach (var s in summaries)
                {
                    foreach (var task in s.DependentTasks ?? new List<PraxisQueuedDependentTask>())
                    {
                        try
                        {
                            EntityBase entity = task.TaskType == nameof(PraxisOpenItem)
                                ? _repository.GetItem<PraxisOpenItem>(r => r.ItemId == task.TaskId)
                                : _repository.GetItem<PraxisProcessGuide>(r => r.ItemId == task.TaskId);
                            task.TaskCreatedBy = entity.CreatedBy;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                                nameof(FixTaskCreatorInReportingCockpitSummary), e.Message, e.StackTrace);
                        }
                    }
                    s.LastUpdateDate = DateTime.UtcNow;
                    await _repository.UpdateAsync(r => r.ItemId == s.ItemId, s);
                    Console.WriteLine($"Script Running... Updated {++index} data out of {totalSummaries}");
                }
                _logger.LogInformation("Task Creator in Reporting Cockpit Summary updated successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixTaskCreatorInReportingCockpitSummary), e.Message, e.StackTrace);

            }
        }
        private async Task FixSerialNumberInMetaDataListForEquipment()
        {
            try
            {
                var collection = _mongoClientRepository.GetCollection(nameof(PraxisEquipment));

                var filter = Builders<BsonDocument>.Filter.And(
                   Builders<BsonDocument>.Filter.Or(
                       Builders<BsonDocument>.Filter.Eq("MetaDataList", BsonNull.Value),
                       Builders<BsonDocument>.Filter.Exists("MetaDataList", false)
                   ),
                   Builders<BsonDocument>.Filter.Ne("SerialNumber", BsonNull.Value),
                   Builders<BsonDocument>.Filter.Ne("SerialNumber", "")
               );

                var projection = Builders<BsonDocument>.Projection.Include("_id").Include("SerialNumber");

                var documents = await collection.Find(filter).Project(projection).ToListAsync();

                var updates = new List<UpdateOneModel<BsonDocument>>();

                foreach (var document in documents)
                {
                    var serialNumber = document["SerialNumber"].AsString;

                    var metaDataList = new BsonArray
                    {
                        new BsonDocument
                        {
                            { "Key", ReportConstants.EquipmentMetaDataKeys.SerialNumber },
                            { "MetaData", new BsonDocument { { "Type", "String" }, { "Value", serialNumber } } }
                        }
                    };

                    var update = Builders<BsonDocument>.Update.Set("MetaDataList", metaDataList);

                    updates.Add(new UpdateOneModel<BsonDocument>(
                        Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                        update
                    ));
                }

                if (updates.Count > 0)
                {
                    await collection.BulkWriteAsync(updates);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixSerialNumberInMetaDataListForEquipment), ex.Message, ex.StackTrace);
            }
        }

        private async Task FixPraxisFormForDeptUser()
        {
            try
            {

                var praxisForms = _repository
                .GetItems<PraxisForm>(p =>
                    !p.IsMarkedToDelete &&
                    ((p.OrganizationIds != null && p.OrganizationIds.Any()) ||
                     p.ProcessGuideCheckList.Any(pg => ((p.OrganizationIds != null && p.OrganizationIds.Any())))))
                ?.ToList();
                if (praxisForms != null)
                {
                    foreach (var item in praxisForms)
                    {

                        var createdByUser = await _repository.GetItemAsync<PraxisUser>(u => u.UserId == item.CreatedBy);
                        if (createdByUser != null && !createdByUser.Roles.Contains(RoleNames.AdminB))
                        {
                            // apply logic later
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixSerialNumberInMetaDataListForEquipment), ex.Message, ex.StackTrace);
            }
        }
        private async Task FixMaintenanceDatesForForEquipment()
        {
            try
            {

                var praxisEquipments = _repository
                .GetItems<PraxisEquipment>(p =>
                    !p.IsMarkedToDelete &&
                    p.MaintenanceDates != null && p.MaintenanceDates.Any()
                     )
                ?.ToList();
                if (praxisEquipments != null)
                {
                    foreach (var item in praxisEquipments)
                    {
                        var maintenancesIds = item.MaintenanceDates.Select(x => x.ItemId).ToList();
                        var maintenances = _repository.GetItems<PraxisEquipmentMaintenance>(x => maintenancesIds.Contains(x.ItemId)).ToList();

                        if (maintenances != null && item.MaintenanceDates.Any())
                        {
                            var maintenanceDates = item.MaintenanceDates.Select(x => new MaintenanceDatePropWithType()
                            {
                                ItemId = x.ItemId,
                                Date = x.Date,
                                CompletionStatus = x.CompletionStatus,
                                ScheduleType = maintenances.Find(m => m.ItemId == x.ItemId)?.ScheduleType
                            });
                            var data = new MetaDataKeyPairValue
                            {
                                Key = EquipmentMetaDataKeys.MaintenanceDates,
                                MetaData = new MetaValuePair
                                {
                                    Type = "Array",
                                    Value = System.Text.Json.JsonSerializer.Serialize(maintenanceDates)
                                }
                            };
                            if (item.MetaDataList != null && item.MetaDataList.Any())
                            {
                                item.MetaDataList.Add(data);
                            }
                            else
                            {
                                var medataList = new List<MetaDataKeyPairValue>()
                                    {
                                        data
                                    };
                                item.MetaDataList = medataList;
                            }
                        }

                        await _repository.UpdateAsync(s => s.ItemId == item.ItemId, item);

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixSerialNumberInMetaDataListForEquipment), ex.Message, ex.StackTrace);
            }
        }

        private async Task AddProcessGuideAnswerMetaData()
        {
            try
            {
                var collection = _mongoClientRepository.GetCollection("PraxisProcessGuideAnswer");

                var filterSetMetaData = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Ne("Answers", BsonNull.Value),
                    Builders<BsonDocument>.Filter.Or(
                        Builders<BsonDocument>.Filter.Eq("Answers.MetaDataList", BsonNull.Value),
                        Builders<BsonDocument>.Filter.Exists("Answers.MetaDataList", false)
                    )
                );

                var updateSetMetaData = Builders<BsonDocument>.Update.Set("Answers.$[].MetaDataList", new BsonArray());
                await collection.UpdateManyAsync(filterSetMetaData, updateSetMetaData);

                var filterPushMetaData = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Ne("Answers", BsonNull.Value),
                    Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.ElemMatch<BsonDocument>(
                            "Answers.MetaDataList",
                            Builders<BsonDocument>.Filter.Eq("Key", "IsAnswerSubmitted")
                        )
                    )
                );

                var updatePushMetaData = Builders<BsonDocument>.Update.Push("Answers.$[].MetaDataList", new BsonDocument
                    {
                        { "Key", "IsAnswerSubmitted" },
                        { "MetaData", new BsonDocument
                            {
                                { "Type", "Boolean" },
                                { "Value", "true" }
                            }
                        }
                    });

                await collection.UpdateManyAsync(filterPushMetaData, updatePushMetaData);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(AddProcessGuideAnswerMetaData), ex.Message, ex.StackTrace);
            }
        }

        private async Task FixObjectArtifactPermissions()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixRiqsPediaDeletedUsers));
            try
            {
                var objectArtifacts = _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete).ToList();

                foreach (var artifact in objectArtifacts)
                {
                    var ids = new List<string>();

                    if (!string.IsNullOrEmpty(artifact.OwnerId)) ids.Add(artifact.OwnerId);
                    var pids = artifact.SharedOrganizationList?.SelectMany(s => s.SharedPersonList ?? new List<string>())?.Distinct()?.ToList() ?? new List<string>();

                    var userIds = pids.Count > 0 ? _objectArtifactUtilityService.GetPraxisUsersByIds(pids.ToArray()).Select(pu => pu.UserId).ToList() : new List<string>();
                    ids.AddRange(userIds);

                    artifact.IdsAllowedToRead = (artifact.IdsAllowedToRead?.ToList() ?? new List<string>()).Union(ids).ToArray();
                    artifact.IdsAllowedToUpdate = (artifact.IdsAllowedToUpdate?.ToList() ?? new List<string>()).Union(ids).ToArray();
                    artifact.IdsAllowedToDelete = (artifact.IdsAllowedToDelete?.ToList() ?? new List<string>()).Union(ids).ToArray();

                    await _repository.UpdateAsync(a => a.ItemId == artifact.ItemId, artifact);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixRiqsPediaDeletedUsers), e.Message, e.StackTrace);
            }
        }

        private async Task FixNonStandardLibraryRights()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixRiqsPediaDeletedUsers));
            try
            {
                var rights = _repository.GetItems<RiqsLibraryControlMechanism>(o => !o.IsMarkedToDelete && o.ControlMechanismName != LibraryControlMechanismConstant.Standard).ToList();

                foreach (var right in rights)
                {
                    var ids = right.UploadAdmins ?? new List<UserPraxisUserIdPair>();
                    ids.AddRange(right.ApprovalAdmins ?? new List<UserPraxisUserIdPair>());
                    ids = ids.DistinctBy(x => x.UserId).ToList();
                    right.UploadAdmins = ids;
                    right.ApprovalAdmins = ids;
                    await _repository.UpdateAsync(a => a.ItemId == right.ItemId, right);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixRiqsPediaDeletedUsers), e.Message, e.StackTrace);
            }
        }

        private async Task FixTranslationInResourceKeys(ResolveProdDataIssuesPayload command)
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixTranslationInResourceKeys));
            try
            {
                int pageSize = command.PageSize;
                string fromLanguage = "de";
                string targetLanguage = command?.TargetLanguage ?? string.Empty;

                if (!string.IsNullOrEmpty(targetLanguage))
                {
                    var collection = _mongoClientRepository.GetCollection(nameof(UilmResourceKey));
                    var filter = Builders<BsonDocument>.Filter.Empty;

                    if (!string.IsNullOrEmpty(command.Filter))
                    {
                        var additionalBsonFilter = BsonSerializer.Deserialize<BsonDocument>(command.Filter);
                        var additionalFilter = new BsonDocumentFilterDefinition<BsonDocument>(additionalBsonFilter);

                        filter = Builders<BsonDocument>.Filter.And(filter, additionalFilter);
                    }

                    var totalSize = await collection.CountDocumentsAsync(filter);
                    var totalPages = (int)Math.Ceiling((double)totalSize / pageSize);

                    if (command.PageNumber > 0)
                    {
                        totalPages = command.PageNumber;
                    }
                    else
                    {
                        command.PageNumber = 1;
                    }

                    for (int currentPage = command.PageNumber; currentPage <= totalPages; currentPage++)
                    {
                        int pageNumber = Math.Max(1, Math.Min(currentPage, totalPages));

                        var documents = await collection.Find(filter)
                                                        .Skip((pageNumber - 1) * pageSize)
                                                        .Limit(pageSize)
                                                        .ToListAsync();

                        var uilmResourceKeys = documents.Select(doc => BsonSerializer.Deserialize<UilmResourceKey>(doc)).ToList();

                        if (uilmResourceKeys.Count > 0)
                        {
                            var textsList = new List<string>();
                            var customPromptList = new List<string>();

                            foreach (var resourceKey in uilmResourceKeys)
                            {
                                var englishText = resourceKey.Resources?.FirstOrDefault(r => r.Culture == "en")?.Value;
                                var sourceLanguageText = resourceKey.Resources?.FirstOrDefault(r => r.Culture == fromLanguage)?.Value;

                                textsList.Add(sourceLanguageText);

                                var prompt = $"en: {englishText}\nde: {sourceLanguageText}\nKeep the word if both English and German are identical.";
                                customPromptList.Add(prompt);
                            }

                            if (textsList?.Count > 0)
                            {
                                var payload = new TranslationMultiplePayload()
                                {
                                    text = textsList,
                                    target_languages = new List<string> { targetLanguage },
                                    model_name = TranslationConst.AzureGPT4oMini,
                                    current_language = "de",
                                    custom_prompt = customPromptList,
                                    read_from_cache = false,
                                    save_to_cache = false
                                };

                                var results = await _translationService.GetTranslationMultiple(payload);

                                foreach (var item in uilmResourceKeys)
                                {
                                    var fromLanguageValue = item.Resources?.FirstOrDefault(x => x.Culture == fromLanguage)?.Value;

                                    if (!string.IsNullOrEmpty(fromLanguageValue) && results?.Any(t => t.text == fromLanguageValue) == true)
                                    {
                                        var updateTargetLanguage = item.Resources.FirstOrDefault(x => x.Culture == targetLanguage);
                                        var translatedText = results.FirstOrDefault(t => t.text == fromLanguageValue)?.translates?.FirstOrDefault()?.translation;
                                        if (updateTargetLanguage != null)
                                        {
                                            _logger.LogInformation(
                                               "Key: {key} -- Context (De): {german} /...... Prev: {prev} -- new: {new}",
                                               item.KeyName, fromLanguageValue, updateTargetLanguage.Value, translatedText
                                           );
                                            updateTargetLanguage.Value = translatedText;
                                        }
                                    }
                                }
                            }

                            var bulkOperations = uilmResourceKeys.Select(resource =>
                            {
                                var filter = Builders<BsonDocument>.Filter.Eq("_id", resource.ItemId);
                                var update = Builders<BsonDocument>.Update.Set("Resources", new BsonArray(resource.Resources.Select(r => r.ToBsonDocument())));
                                return new UpdateOneModel<BsonDocument>(filter, update);
                            }).ToList();

                            if (bulkOperations.Count > 0)
                            {
                                var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);

                                _logger.LogInformation(
                                   "Bulk update completed: {MatchedCount} matched, {ModifiedCount} modified, {InsertedCount} inserted.",
                                   bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, bulkWriteResult.Upserts.Count
                               );
                            }
                        }

                        await Task.Delay(2000); // 2-second delay before fetching the next page
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixTranslationInResourceKeys), e.Message, e.StackTrace);
            }
        }

        private async Task FixRightsToEquipmentTable()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixRightsToEquipmentTable));
            try
            {
                var rights = _repository.GetItems<PraxisEquipmentRight>(o => !o.IsMarkedToDelete && !string.IsNullOrEmpty(o.EquipmentId)).ToList();

                foreach (var right in rights)
                {
                    if (!string.IsNullOrEmpty(right?.EquipmentId))
                    {
                        var equipment = await _repository.GetItemAsync<PraxisEquipment>(e => !e.IsMarkedToDelete && e.ItemId == right.EquipmentId);
                        if (equipment != null)
                        {
                            var metaValues = equipment.MetaValues?.Where(m => m.Key != "AssignedRights").ToList() ?? new List<PraxisKeyValue>();
                            var adminIds = right.AssignedAdmins?.Select(a => a.PraxisUserId)?.ToList() ?? new List<string>();
                            var praxisUsers = _repository.GetItems<PraxisUser>(pu => !pu.IsMarkedToDelete && adminIds.Contains(pu.ItemId))?.ToList() ?? new List<PraxisUser>();
                            var assignedRights = adminIds.Select(id => new
                            {
                                PraxisUserId = id,
                                Name = praxisUsers?.FirstOrDefault(pu => pu.ItemId == id)?.DisplayName
                            }).ToList();

                            if (assignedRights?.Count > 0)
                            {
                                metaValues.Add(new PraxisKeyValue()
                                {
                                    Key = "AssignedRights",
                                    Value = JsonConvert.SerializeObject(assignedRights)
                                });
                            }

                            equipment.MetaValues = metaValues;

                            await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixRightsToEquipmentTable), e.Message, e.StackTrace);
            }
        }

        private async Task FixObjectArifactSortableField()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixObjectArifactSortableField));
            try
            {
                var artifacts = _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete && o.ArtifactType == ArtifactTypeEnum.File).ToList();
                var mappings = _repository.GetItems<RiqsObjectArtifactMapping>(o => !o.IsMarkedToDelete).ToList();

                foreach (var artifact in artifacts)
                {
                    var isUpdate = false;
                    var mapping = mappings.FirstOrDefault(m => m.ObjectArtifactId == artifact.ItemId);
                    if (!string.IsNullOrEmpty(mapping?.ItemId) && artifact.MetaData != null)
                    {
                        if (mapping.ApproverInfos?.Last(a => a.ApprovedDate.Year >= 1000)?.ApprovedDate != null)
                        {
                            var approvedDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.APPROVED_DATE.ToString()];
                            var approvedDateValue = new MetaValuePair()
                            {
                                Type = "string",
                                Value = mapping.ApproverInfos.Last(a => a.ApprovedDate.Year >= 1000)?.ApprovedDate.ToString("o", CultureInfo.InvariantCulture)
                            };
                            if (artifact.MetaData.TryGetValue(approvedDateKey, out _))
                            {
                                artifact.MetaData[approvedDateKey] = approvedDateValue;
                            }
                            else
                            {
                                artifact.MetaData.Add(approvedDateKey, approvedDateValue);
                            }
                            isUpdate = true;
                        }
                    }


                    if (_objectArtifactUtilityService.IsADocument(artifact.MetaData))
                    {
                        var documentMapping = await _repository.GetItemAsync<DocumentEditMappingRecord>(d => d.ObjectArtifactId == artifact.ItemId);
                        var editedDate = documentMapping?.EditHistory?.OrderByDescending(e => e.EditDate)?.FirstOrDefault()?.EditDate;

                        if (editedDate != null)
                        {
                            var editedDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DOCUMENT_EDITED_DATE.ToString()];
                            var editedDateValue = new MetaValuePair()
                            {
                                Type = "string",
                                Value = editedDate.Value.ToString("o", CultureInfo.InvariantCulture)
                            };
                            if (artifact.MetaData.TryGetValue(editedDateKey, out _))
                            {
                                artifact.MetaData[editedDateKey] = editedDateValue;
                            }
                            else
                            {
                                artifact.MetaData.Add(editedDateKey, editedDateValue);
                            }
                            isUpdate = true;
                        }

                    }

                    if (isUpdate) await _repository.UpdateAsync(e => e.ItemId == artifact.ItemId, artifact);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixObjectArifactSortableField), e.Message, e.StackTrace);
            }
        }

        private async Task FixUserCount()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixUserCount));
            try
            {
                var clients = _repository.GetItems<PraxisClient>(o => !o.IsMarkedToDelete && !string.IsNullOrEmpty(o.ParentOrganizationId)).ToList();
                foreach (var client in clients)
                {
                    await _userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(client.ItemId, client.ParentOrganizationId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixUserCount), e.Message, e.StackTrace);
            }
        }

        private async Task FixEquipmentLocationLogRemarks()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixEquipmentLocationLogRemarks));
            try
            {
                var praxisRooms = _repository.GetItems<PraxisRoom>(o => !o.IsMarkedToDelete && !string.IsNullOrEmpty(o.Remarks)).ToList();
                var locationHistories = _repository.GetItems<PraxisEquipmentLocationHistory>(o => !o.IsMarkedToDelete && string.IsNullOrEmpty(o.Remarks)).ToList();

                var updates = new List<WriteModel<BsonDocument>>();

                foreach (var locationHistory in locationHistories)
                {
                    var matchingLocation = praxisRooms.FirstOrDefault(room => room.ItemId == locationHistory?.LocationChangeLog?.CurrentLocationInfo?.LocationId);

                    if (matchingLocation != null && !string.IsNullOrEmpty(matchingLocation.Remarks))
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", locationHistory.ItemId);
                        var update = Builders<BsonDocument>.Update.Set("Remarks", matchingLocation.Remarks);

                        updates.Add(new UpdateOneModel<BsonDocument>(filter, update));
                    }
                }

                if (updates != null && updates.Count() > 0)
                {
                    var collection = _mongoClientRepository.GetCollection("PraxisEquipmentLocationHistorys");
                    var bulkWriteResult = await collection.BulkWriteAsync(updates);

                    _logger.LogInformation("Bulk update completed. Matched: {MatchedCount}, Modified: {ModifiedCount}",
                        bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixEquipmentLocationLogRemarks), e.Message, e.StackTrace);
            }
        }

        private async Task FixProcessGuideDates()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixProcessGuideDates));
            try
            {
                var pgs = _repository.GetItems<PraxisProcessGuide>(o => !o.IsMarkedToDelete && o.IsAClonedProcessGuide).ToList();
                var configs = _repository.GetItems<PraxisProcessGuideConfig>(o => !o.IsMarkedToDelete).ToList();

                foreach (var pg in pgs)
                {
                    pg.PatientDateOfBirth = pg.PatientDateOfBirth.ToUniversalTime().Date;
                    await _repository.UpdateAsync(o => o.ItemId == pg.ItemId, pg);

                }

                foreach (var config in configs)
                {
                    config.PatientDateOfBirth = config.PatientDateOfBirth.ToUniversalTime().Date;
                    await _repository.UpdateAsync(o => o.ItemId == config.ItemId, config);

                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixProcessGuideDates), e.Message, e.StackTrace);
            }
        }

        private async Task FixArtifactWritePermission()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixArtifactWritePermission));
            try
            {
                var artifacts = _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete).ToList();

                foreach (var artifact in artifacts)
                {
                    if (artifact.ItemId == artifact.OrganizationId) continue;
                    if (_objectArtifactUtilityService.IsAForm(artifact.MetaData) && !_objectArtifactUtilityService.IsACompletedFormResponse(artifact.MetaData)) continue;

                    var level = "0";
                    var authorizedRoles = new string[] {};
                    var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact, onlyDeptLevel: _objectArtifactUtilityService.IsASecretArtifact(artifact.MetaData));

                    var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(artifact.MetaData);
                    var department = _objectArtifactUtilityService.GetDepartmentById(departmentId);

                    if (_objectArtifactPermissionHelperService.IsAAdminBUpload(artifact.CreatedBy, artifact.OrganizationId) || department == null)
                    {
                        level = "1";
                        authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(artifact.OrganizationId);
                    }
                    else if ((_objectArtifactPermissionHelperService.IsALibraryAdminUpload(artifact, artifact.CreatedBy) 
                        && _objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType)) || _objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType))
                    {
                        level = "1";
                        authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(artifact.OrganizationId);
                    }
                    else if (department != null)
                    {
                        authorizedRoles = _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRoles(artifact.OrganizationId, department.ItemId);
                    }
                    var sharedReadIds = _objectArtifactShareService.GetSharedIdsAllowedToRead(artifact.SharedOrganizationList);
                    var sharedUpdateIds = _objectArtifactShareService.GetSharedIdsAllowedToUpdate(artifact.SharedOrganizationList);

                    artifact.IdsAllowedToRead = sharedReadIds.Union(authorizedIds).Distinct().ToArray();
                    artifact.IdsAllowedToUpdate = sharedUpdateIds.Union(authorizedIds).Distinct().ToArray();

                    artifact.RolesAllowedToWrite = authorizedRoles;
                    artifact.IdsAllowedToWrite = authorizedIds;

                    artifact.IdsAllowedToDelete = (artifact.IdsAllowedToDelete ?? new string[] {}).Concat(authorizedIds).Distinct().ToArray();
                    artifact.RolesAllowedToDelete = (artifact.RolesAllowedToDelete ?? new string[] { }).Concat(authorizedRoles).Distinct().ToArray();

                    artifact.MetaData ??= new Dictionary<string, MetaValuePair>();
                    artifact.MetaData["IsOrgLevel"] = new MetaValuePair() { Type = "string", Value = level };

                    await _repository.UpdateAsync(o => o.ItemId == artifact.ItemId, artifact);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixArtifactWritePermission), e.Message, e.StackTrace);
            }
        }

        private async Task FixEquipmentQrCode()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixEquipmentQrCode));
            try
            {
                var equipments = _repository.GetItems<PraxisEquipment>(o => !o.IsMarkedToDelete).ToList();
                _logger.LogInformation("Total Equipment Count: {Count}", equipments.Count);
                foreach (var equipment in equipments)
                {
                    await _praxisEquipmentService.GenerateQrFileForEquipment(equipment);
                }
                _logger.LogInformation("Equipment QrCode Generation Completed Successfully");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixEquipmentQrCode), e.Message, e.StackTrace);
            }
        }

        private async Task FixPraxisFormOwnerRole()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixPraxisFormOwnerRole));
            try
            {
                var forms = _repository.GetItems<PraxisForm>(o => !o.IsMarkedToDelete).ToList();

                foreach (var form in forms)
                {
                    form.MetaDataList ??= new List<MetaDataKeyPairValue>();
                    var praxisUser = await _repository.GetItemAsync<PraxisUser>(p => p.UserId == form.CreatedBy);
                    if (praxisUser == null) continue;
                    var role = GetRoleByHierarchy(praxisUser.Roles?.ToList());

                    if (role == string.Empty) continue;
                    var bossRoles = new string[] { RoleNames.Admin, RoleNames.GroupAdmin, RoleNames.AdminB };
                    if (!bossRoles.Contains(role))
                    {
                        var clientIds = form?.ProcessGuideCheckList?.SelectMany(p => p.ClientInfos?.Select(c => c.ClientId)?.ToList() ?? new List<string>())?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>();
                        clientIds.AddRange(
                            form?.ProcessGuideCheckList?.Select(p => p.ClientId)?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>()
                        );
                        if (!string.IsNullOrEmpty(form.ClientId)) clientIds.Add(form.ClientId);
                        clientIds.AddRange(
                            form?.ClientInfos?.Select(p => p.ClientId)?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>()
                        );

                        clientIds = clientIds.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

                        var orgIds = form?.ProcessGuideCheckList?.SelectMany(p => p.OrganizationIds?.ToList() ?? new List<string>())?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>();
                        orgIds.AddRange(
                            form?.OrganizationIds?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>()
                        );
                        orgIds.AddRange(
                            form?.ProcessGuideCheckList?.SelectMany(p => p.ClientInfos?.Select(c => c.ParentOrganizationId)?.ToList() ?? new List<string>())?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>()
                        );
                        orgIds.AddRange(
                            form?.ClientInfos?.Select(p => p.ParentOrganizationId)?.Where(c => !string.IsNullOrEmpty(c))?.ToList() ?? new List<string>()
                        );

                        orgIds = orgIds.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

                        if (
                            orgIds.Count > 0 || clientIds.Count > 1
                        )
                        {
                            role = orgIds.Count > 1 ? RoleNames.GroupAdmin : RoleNames.AdminB;
                        }
                        else
                        {
                            if (clientIds.Count == 0) continue;

                            var roles = praxisUser.ClientList?.FirstOrDefault(c => c.ClientId == clientIds[0])?.Roles;
                            role = GetRoleByHierarchy(roles?.ToList());

                            if (role == string.Empty) continue;
                        }
                    } 
                    var roleMetaData = form.MetaDataList.Find(m => m.Key == "OwnerRole");
                    if (roleMetaData != null) roleMetaData.MetaData = new MetaValuePair() { Type = "string", Value = role };
                    else
                    {
                        form.MetaDataList.Add(new MetaDataKeyPairValue()
                        {
                            Key = "OwnerRole",
                            MetaData = new MetaValuePair() { Type = "string", Value = role }
                        });
                    }

                    await _repository.UpdateAsync(o => o.ItemId == form.ItemId, form);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixPraxisFormOwnerRole), e.Message, e.StackTrace);
            }
        }

        private string GetRoleByHierarchy(List<string> roles)
        {
            if (roles == null) return string.Empty;
            if (roles.Contains(RoleNames.Admin)) return RoleNames.Admin;
            else if (roles.Contains(RoleNames.SystemAdmin)) return RoleNames.SystemAdmin;
            else if (roles.Contains(RoleNames.TaskController)) return RoleNames.TaskController;
            else if (roles.Contains(RoleNames.ExternalUser)) return RoleNames.ExternalUser;
            else if (roles.Contains(RoleNames.GroupAdmin)) return RoleNames.GroupAdmin;
            else if (roles.Contains(RoleNames.AdminB)) return RoleNames.AdminB;
            else if (roles.Contains(RoleNames.PowerUser)) return RoleNames.PowerUser;
            else if (roles.Contains(RoleNames.Leitung)) return RoleNames.Leitung;
            else if (roles.Contains(RoleNames.MpaGroup1)) return RoleNames.MpaGroup1;
            else if (roles.Contains(RoleNames.MpaGroup2)) return RoleNames.MpaGroup2;
            return string.Empty;
        }



        private void FixPraxisFormPermission()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixPraxisFormPermission));
            try
            {
                var forms = _repository.GetItems<PraxisForm>(o => !o.IsMarkedToDelete).ToList();

                foreach (var form in forms)
                {
                    if (!string.IsNullOrEmpty(form.PurposeOfFormKey) && form.PurposeOfFormKey.Equals("process-guide"))
                    {
                        var clientInfos = form?.ProcessGuideCheckList?
                            .Where(f => f.ClientInfos != null)?
                            .SelectMany(f => f.ClientInfos)
                            .ToList() ?? new List<FormSpecificClientInfo>();
                        var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                        var orgIds = form?.ProcessGuideCheckList?.FirstOrDefault(f => f.OrganizationIds != null && f.OrganizationIds.Count > 0)?.OrganizationIds;
                        _praxisFormService.AddRowLevelSecurity(form.ItemId, clientIds, orgIds);
                    }
                    else
                    {
                        var clientInfos = form?.ClientInfos?.ToList() ?? new List<FormSpecificClientInfo>();
                        var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                        var orgIds = form?.OrganizationIds;
                        _praxisFormService.AddRowLevelSecurity(form.ItemId, clientIds, orgIds);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixPraxisFormPermission), e.Message, e.StackTrace);
            }
        }

        private async Task RemoveDeletedCirsCockpitTask(ResolveProdDataIssuesPayload command)
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(RemoveDeletedCirsCockpitTask));
            try
            {
                var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<RiqsTaskCockpitSummary>($"{nameof(RiqsTaskCockpitSummary)}s");
                var builder = Builders<RiqsTaskCockpitSummary>.Filter;
                var filter = builder.Eq(r => r.RelatedEntityName, CockpitTypeNameEnum.CirsGenericReport) &
                             builder.Eq(r => r.IsMarkedToDelete, false) &
                             builder.Eq(r => r.IsTaskCompleted, false) &
                             builder.Eq(r => r.IsSummaryHidden, false);
                var activeTasks = await collection.Find(filter).ToListAsync();


                if (!activeTasks.Any())
                {
                    _logger.LogInformation("No active CIRS tasks found");
                    return;
                }

                var activeTaskIds = activeTasks.Select(t => t.RelatedEntityId).ToList();

                var existingReports = _repository
                    .GetItems<CirsGenericReport>(report =>
                        activeTaskIds.Contains(report.ItemId) &&
                        report.IsActive &&
                        !report.IsMarkedToDelete)
                    .Select(r => r.ItemId)
                    .ToList();

                var orphanedTasks = activeTaskIds.Except(existingReports).ToList();

                if (orphanedTasks.Any())
                {
                    var deletedCount = await collection.DeleteManyAsync(task =>
                                orphanedTasks.Contains(task.RelatedEntityId) &&
                                task.RelatedEntityName == CockpitTypeNameEnum.CirsGenericReport);

                    _logger.LogInformation("Removed {Count} orphaned CIRS cockpit tasks", deletedCount.DeletedCount);
                }
                else
                {
                    _logger.LogInformation("No orphaned CIRS tasks found");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(RemoveDeletedCirsCockpitTask), e.Message, e.StackTrace);
            }
        }

        private async Task FixPraxisActivePraxisUser()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixPraxisActivePraxisUser));
            try
            {
                var pus = _repository.GetItems<PraxisUser>(o => !o.IsMarkedToDelete && !o.Active).ToList();

                foreach (var pu in pus)
                {
                    var user = _repository.GetItems<User>(o => !o.IsMarkedToDelete && o.Active && o.ItemId == pu.UserId).ToList();
                    if (user != null)
                    {
                        pu.Active = true;
                        await _repository.UpdateAsync(p => p.ItemId == pu.ItemId, pu);
                    }
                }
                await FixUserCount();
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixPraxisActivePraxisUser), e.Message, e.StackTrace);
            }
        }

        private async Task FixPraxisFormSharedOrgProperty()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixPraxisFormSharedOrgProperty));
            try
            {
                var forms = _repository.GetItems<PraxisForm>(o => !o.IsMarkedToDelete).ToList();

                foreach (var form in forms)
                {
                    if (!string.IsNullOrEmpty(form.OrganizationId)) form.OrganizationIds = new List<string> { form.OrganizationId };
                    var clientInfos = form.ClientInfos?.ToList() ?? new List<FormSpecificClientInfo>();

                    foreach (var clientInfo in clientInfos)
                    {
                        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientInfo.ClientId);
                        clientInfo.ParentOrganizationId = client?.ParentOrganizationId;
                    }
                    form.ClientInfos = clientInfos;

                    var pgCheckList = form.ProcessGuideCheckList?.ToList() ?? new List<ClientSpecificCheckList>();
                    foreach (var pgCheck in pgCheckList)
                    {

                        if (!string.IsNullOrEmpty(pgCheck.OrganizationId)) pgCheck.OrganizationIds = new List<string> { pgCheck.OrganizationId };
                        clientInfos = pgCheck.ClientInfos?.ToList() ?? new List<FormSpecificClientInfo>();

                        foreach (var clientInfo in clientInfos)
                        {
                            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientInfo.ClientId);
                            clientInfo.ParentOrganizationId = client?.ParentOrganizationId;
                        }
                        pgCheck.ClientInfos = clientInfos;
                    }

                    form.ProcessGuideCheckList = pgCheckList;

                    await _repository.UpdateAsync(o => o.ItemId == form.ItemId, form);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixPraxisFormSharedOrgProperty), e.Message, e.StackTrace);
            }
        }

        private async Task FixNextRequiredStatusForGeneratedReportTemplate()
        {
            _logger.LogInformation("Entered into method : {MethodName}",
                nameof(FixNextRequiredStatusForGeneratedReportTemplate));
            try
            {
                var reportTemplates = _repository.GetItems<PraxisGeneratedReportTemplateConfig>(o => !o.IsMarkedToDelete).ToList();
                if (reportTemplates == null || reportTemplates.Count == 0)
                {
                    _logger.LogInformation("No report templates found to update.");
                    return;
                }
                _logger.LogInformation("Total Report Templates Count: {Count}", reportTemplates.Count);

                foreach (var template in reportTemplates)
                {
                    _logger.LogInformation("Processing Report Template: {TemplateId}. Current Status: {Status}", template.ItemId, nameof(template.Status));
                    template.NextStatus = template.Status;
                    await _repository.UpdateAsync(o => o.ItemId == template.ItemId, template);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixNextRequiredStatusForGeneratedReportTemplate), e.Message, e.StackTrace);
            }
            _logger.LogInformation("All report templates processed successfully.");
        }

        private async Task FixLastUpdatedByPropertyInCockpitSummary()
        {
            _logger.LogInformation("Entered into method : {MethodName}", nameof(FixLastUpdatedByPropertyInCockpitSummary));
            try
            {
                var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<BsonDocument>($"{nameof(RiqsTaskCockpitSummary)}s");
                var filter = Builders<BsonDocument>.Filter.Type("LastUpdatedBy", BsonType.Array);
                var documents = await collection.Find(filter).ToListAsync();
                var totalDocuments = documents.Count;
                _logger.LogInformation("Total Documents with LastUpdatedBy as Array: {Count}", totalDocuments);
                var updatedCount = 0;
                foreach (var document in documents)
                {
                    var lastUpdatedBy = document.GetValue("LastUpdatedBy", new BsonArray());
                    BsonValue newLastUpdatedBy = BsonNull.Value;
                    if (lastUpdatedBy is BsonArray bsonArray && bsonArray.Count > 0)
                    {
                        var firstElement = bsonArray[0];
                        if (firstElement.IsString)
                        {
                            newLastUpdatedBy = firstElement.AsString;
                        }
                    }
                    var updatedDocument = Builders<BsonDocument>.Update.Set("LastUpdatedBy", newLastUpdatedBy);
                    await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", document["_id"]), updatedDocument);
                    updatedCount++;
                    _logger.LogInformation("Updated so far: {Index}. Updated Document ID: {Id} with LastUpdatedBy: {LastUpdatedBy}", 
                        updatedCount, document["_id"], newLastUpdatedBy);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Exception in Method: {MethodName}.\nException Message: {Message}.\nException Details: {StackTrace}",
                    nameof(FixLastUpdatedByPropertyInCockpitSummary), e.Message, e.StackTrace);
            }
            _logger.LogInformation("Completed fixing LastUpdatedBy property in Cockpit Summary documents.");
        }

    }
}
