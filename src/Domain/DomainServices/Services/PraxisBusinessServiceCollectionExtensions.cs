using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.WorkSpaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Licensing;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.OpenOrg;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Persona;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.EquipmentReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.RiskOverviewReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services.CustomLogger;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.TwoFactorAuthentication;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.WorkSpaces;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.AssessmentEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientCategoryEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentMaintenanceEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.OrganizationEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisFormEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemConfigEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideAnswerEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisRoomEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskConfigEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingAnswerEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisUserEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.RiskManagementEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.SubscriptionEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.TaskManagementEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Users;
using Selise.Ecap.SC.PraxisMonitor.Domain.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.Notifier;
using SeliseBlocks.MailService.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

public static class PraxisBusinessServiceCollectionExtensions
{
    public static void AddPraxisBusinessServices(this IServiceCollection services)
    {
        services.AddTransient<IMongoClientRepository, MongoClientRepository>();
        services.AddTransient<IMongoDataService, MongoDataService>();
        services.AddTransient<IMongoSecurityService, MongoSecurityService>();
        services.AddTransient<IMailServiceClient, MailServiceClient>();
        services.AddTransient<IEmailNotifierService, EmailNotifierService>();
        services.AddTransient<IPraxisEmailNotifierService, PraxisEmailNotifierService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddSingleton<IChangeLogService, ChangeLogService>();
        services.AddSingleton<IPraxisClientCategoryService, PraxisClientCategoryService>();
        services.AddSingleton<IPraxisUserService, PraxisUserService>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IPraxisClientService, PraxisClientService>();
        services.AddSingleton<IPraxisTrainingService, PraxisTrainingService>();
        services.AddSingleton<IPraxisTrainingAnswerService, PraxisTrainingAnswerService>();
        services.AddSingleton<IPraxisTaskConfigService, PraxisTaskConfigService>();
        services.AddSingleton<IPraxisTaskService, PraxisTaskService>();
        services.AddSingleton<ITaskManagementService, TaskManagementService>();
        services.AddSingleton<IUserPersonService, UserPersonService>();
        services.AddSingleton<IPraxisRiskService, PraxisRiskService>();
        services.AddSingleton<IPraxisAssessmentService, PraxisAssessmentService>();
        services.AddSingleton<IPraxisEquipmentService, PraxisEquipmentService>();
        services.AddSingleton<IPraxisEquipmentMaintenanceService, PraxisEquipmentMaintenanceService>();
        services.AddSingleton<IPraxisRoomService, PraxisRoomService>();
        services.AddSingleton<IPraxisOpenItemService, PraxisOpenItemService>();
        services.AddSingleton<IExportReportService, ExportReportService>();
        services.AddSingleton<IStorageDataService, StorageDataService>();
        services.AddSingleton<ISchedulerService, SchedulerService>();
        services.AddSingleton<ICommonUtilService, CommonUtilService>();
        services.AddSingleton<IPraxisProcessGuideService, PraxisProcessGuideService>();
        services.AddSingleton<IPraxisProcessGuideDeleteService, PraxisProcessGuideDeleteService>();
        services.AddSingleton<IPraxisProcessGuideAnswerService, PraxisProcessGuideAnswerService>();
        services.AddSingleton<ICreateDynamicLink, CreateDynamicLinkService>();
        services.AddSingleton<IUilmResourceKeyService, UilmResourceKeyService>();
        services.AddSingleton<IPraxisShiftService, PraxisShiftService>();
        services.AddSingleton<IGenerateShiftPlanReportService, GenerateShiftPlanReportService>();
        services.AddSingleton<IDocumentEditHistoryService, DocumentEditHistoryService>();
        services.AddSingleton<IShiftTaskAssignService, ShiftTaskAssignService>();
        services.AddSingleton<IPraxisShiftPermissionService, PraxisShiftPermissionService>();
        services.AddSingleton<ISubscriptionUtilityService, SubscriptionUtilityService>();
        services.AddSingleton<ISubscriptionUpdateService, SubscriptionUpdateService>();
        services.AddSingleton<ISubscriptionRenewalService, SubscriptionRenewalService>();
        services.AddSingleton<IHtmlFromTemplateGeneratorService, HtmlFromTemplateGeneratorService>();
        services.AddSingleton<IDependencyManagementService, DependencyManagementService>();
        services.AddSingleton<IPraxisClientsForReportingQueryService, PraxisClientsForReportingQueryService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<TwoFactorAuthenticationService>();
        services.AddSingleton<AnonymousUserTwoFactorAuthenticationService>();
        services.AddSingleton<ITwoFactorAuthenticationServiceFactory, TwoFactorAuthenticationServiceFactory>();
        services.AddSingleton<ITwoFactorAuthenticationServiceFactory>(ctx =>
        {
            var factories = new Dictionary<TwoFactorType, Func<ITwoFactorAuthenticationService>>()
            {
                [TwoFactorType.Anonymous] = () => ctx.GetService<AnonymousUserTwoFactorAuthenticationService>(),
                [TwoFactorType.System] = () => ctx.GetService<TwoFactorAuthenticationService>(),

            };
            return new TwoFactorAuthenticationServiceFactory(factories);
        });
        services.AddSingleton<IAssignEquipmentAdminsService, AssignEquipmentAdminsService>();
        services.AddSingleton<IPraxisEquipmentQueryService, PraxisEquipmentQueryService>();
        services.AddSingleton<IEquipmentReportGenerationStrategyService, EquipmentReportGenerationStrategyService>();
        services.AddSingleton<IActivateUserAccount, ActivateUserAccountService>();
        services.AddSingleton<INavigationPreparationTypeStrategy, NavigationPreparationTypeStrategyService>();
        services.AddSingleton<InsertDynamicNavigationService>();
        services.AddSingleton<UpdateDynamicNavigationService>();
        services.AddSingleton<IPrepareNavigationRoleByOrganization, PrepareNavigationRoleByOrganizationService>();
        services.AddSingleton<IInsertNavigationRolesToRoleHierarchy, InsertNavigationRolesToRoleHierarchyService>();
        services.AddSingleton<IDocumentUploadAndConversion, DocumentUploadAndConversionService>();
        services.AddSingleton<IDmsService, DmsService>();
        services.AddSingleton<ILincensingService, LincensingService>();
        services.AddSingleton<IAuthUtilityService, AuthUtilityService>();
        services.AddSingleton<IEmailDataBuilder, EmailDataBuilder>();
        services.AddSingleton<IPraxisEmailDataBuilders, PraxisEmailDataBuilders>();
        services.AddSingleton<GetCurrentStatusForCategoryData>();
        services.AddSingleton<GetCurrentStatusForSubCategoryData>();
        services.AddSingleton<GetCurrentStatusForTrainingData>();
        services.AddSingleton<GetCurrentStatusForFormCreatorData>();
        services.AddSingleton<GetCurrentStatusForSupplierData>();
        services.AddSingleton<GetCurrentStatusForUserAdditionalInfoTitle>();
        services.AddSingleton<GetCurrentStatusForPraxisRoomData>();
        services.AddSingleton<ICurrentStatusStrategy, CurrentStatusStrategyService>();
        services.AddSingleton<UserUpdateService>();
        services.AddSingleton<ProcessExistingUserDataService>();
        services.AddSingleton<ProcessNewUserDataService>();
        services.AddSingleton<IProcessUserData, ProcessUserDataService>();
        services.AddSingleton<IUserCheckingStrategy, UserCheckingStrategyService>();
        services.AddSingleton<IProcessUserDataByUam, ProcessUserDataByUamService>();
        services.AddSingleton<IUpdateOrgTypeChangePermissionService, UpdateOrgTypeChangePermissionService>();
        services.AddSingleton<IOrganizationDataProcessService, OrganizationDataProcessService>();
        services.AddSingleton<IPreparePersonaRoleMap, PreparePersonaRoleMapService>();
        services.AddSingleton<IPersonaRoleNameService, PersonaRoleNameService>();
        services.AddSingleton<IRoleHierarchyForPersonaRoleService, RoleHierarchyForPersonaRoleService>();
        services.AddSingleton<ISaveDataToPlatformDictionary, SaveDataToPlatformDictionaryService>();
        services.AddSingleton<ISaveDataToFeatureRoleService, SaveDataToFeatureRoleMapService>();
        services.AddSingleton<ISaveDataToPersonaRoleMap, SaveDataToPersonaRoleMapService>();
        services.AddSingleton<IPersonaRolesService, PersonaRolesService>();
        services.AddSingleton<IProcessOpenOrgRole, ProcessOpenOrgRoleService>();
        services.AddSingleton<ISaveDataToFeatureRoleMap, SaveDataToFeatureRoleMap>();
        services.AddSingleton<IPrepareNewRole, PrepareNewRole>();
        services.AddSingleton<IUserRoleService, UserRoleService>();
        services.AddSingleton<ICreateDynamicLink, CreateDynamicLinkService>();
        services.AddSingleton<IPraxisFileService, PraxisFileService>();
        services.AddSingleton<ISequenceNumberService, SequenceNumberService>();
        services.AddSingleton<IPraxisOrganizationExistCheckService, PraxisOrganizationExistService>();
        services.AddSingleton<ISecurityHelperService, SecurityHelperService>();
        services.AddSingleton<IPraxisQrGeneratorService, PraxisQrGeneratorService>();
        services.AddSingleton<IGenerateSuppliersReport, GenerateSuppliersReport>();
        services.AddSingleton<IExternalUserCreateService, ExternalUserCreateService>();
        services.AddSingleton<IPraxisFormService, PraxisFormService>();
        services.AddSingleton<IWorkSpaceService, WorkSpaceService>();
        services.AddSingleton<IAppCatalogueRepositoryService, AppCatalogueRepositoryService>();
        services.AddSingleton<DeleteTaskScheduleDataForPraxisOpenItem>();
        services.AddSingleton<DeleteTaskScheduleDataForPraxisProcessGuide>();

        services.AddSingleton<IPraxisAssignedTaskFormService, PraxisAssignedTaskFormService>();
        services.AddSingleton<IPraxisEntityService, PraxisEntityService>();
        services.AddSingleton<IRiqsInterfaceUserMigrationService, RiqsInterfaceUserMigrationService>(); 
        services.AddSingleton<IRiqsInterfaceSupplierMigrationService, RiqsInterfaceSupplierMigrationService>(); 

        #region Domain Services needed for PraxisClient Deletion
        services.AddSingleton<PraxisUserService>();
        services.AddSingleton<PraxisEquipmentService>();
        services.AddSingleton<PraxisOpenItemService>();
        services.AddSingleton<PraxisRoomService>();
        services.AddSingleton<PraxisRiskService>();
        services.AddSingleton<PraxisFormService>();
        services.AddSingleton<PraxisTaskService>();
        services.AddSingleton<PraxisTrainingService>();
        services.AddSingleton<PraxisProcessGuideService>();
        services.AddSingleton<PraxisTrainingAnswerService>();
        services.AddSingleton<PraxisClientCategoryService>();
        #endregion


        #region Data Deletion
        services.AddSingleton<IDeleteDataStrategy, DeleteDataStrategyService>();
        services.AddSingleton<DeleteDataForPraxisOrganization>();
        services.AddSingleton<DeleteDataForRiqsIncident>();
        services.AddSingleton<DeleteDataForEquipment>();
        services.AddSingleton<DeleteDataForLocation>();
        services.AddSingleton<DeleteDataForEquipmentMaintenance>();
        services.AddSingleton<DeleteDataForTraining>();
        services.AddSingleton<DeleteDataForRiskManagement>();
        services.AddSingleton<DeleteDataForPraxisClient>();
        services.AddSingleton<DeleteDataForPraxisReport>();
        services.AddSingleton<IDeleteDataRoleAndEntitySpecificStrategy, DeleteDataByRoleAndEntitySpecificStrategyService>();
        services.AddSingleton<DeleteRiskDataForSystemAdmin>();
        services.AddSingleton<DeleteRiskDataForClientAdmin>();
        services.AddSingleton<DeleteAssessmentDataForClientAdmin>();
        services.AddSingleton<DeleteAssessmentDataForSystemAdmin>();
        services.AddSingleton<DeleteDataForPraxisAssessment>();
        services.AddSingleton<DeleteCategoryFromPraxisClientCategory>();
        services.AddSingleton<DeleteCategoryDataForSystemAdmin>();
        services.AddSingleton<DeleteCategoryDataForClientAdmin>();
        services.AddSingleton<DeleteSubCategoryFromPraxisClientCategory>();
        services.AddSingleton<DeleteSubCategoryDataForSystemAdmin>();
        services.AddSingleton<DeleteSubCategoryDataForClientAdmin>();
        services.AddSingleton<DeleteDataForFormCreator>();
        services.AddSingleton<DeleteFormCreatorDataAdminAndTaskController>();
        services.AddSingleton<DeleteFormCreatorDataForClientAdmin>();
        services.AddSingleton<DeleteDataForUser>();
        services.AddSingleton<DeleteUserDataAdminAndTaskController>();
        services.AddSingleton<DeleteUserDataForClientAdmin>();
        services.AddSingleton<DeleteUserRelatedData>();
        services.AddSingleton<DeleteDataForProcessGuide>();
        services.AddSingleton<IDeleteUserRelatedData, DeleteUserRelatedData>();
        services.AddSingleton<IRevokePermissionForCommonEntities, RevokePermissionForCommonEntitiesService>();
        services.AddSingleton<IRevokePermissionByRoleStrategy, RevokePermissionByRoleStrategyService>();
        services.AddSingleton<RevokePermissionForPowerUser>();
        services.AddSingleton<RevokePermissionForLeitung>();
        services.AddSingleton<RevokePermissionForEEGroupOne>();
        services.AddSingleton<RevokePermissionForEEGroupTwo>();
        services.AddSingleton<DeleteTaskScheduleDataForPraxisOpenItem>();
        services.AddSingleton<DeleteTaskScheduleDataForPraxisProcessGuide>();
        services.AddSingleton<IDeleteDmsArtifactUsageReferenceService, DeleteDmsArtifactUsageReferenceService>();
        #endregion

        services.AddSingleton<IPrepareNavigationRoleByOrganization, PrepareNavigationRoleByOrganizationService>();
        services.AddSingleton<IInsertNavigationRolesToRoleHierarchy, InsertNavigationRolesToRoleHierarchyService>();
        services.AddSingleton<ISaveDataToArchivedRole, SaveDataToArchivedRoleService>();
        services.AddSingleton<IUpdateDeletePermissionForOpenOrg, UpdateDeletePermissionForOpenOrgService>();
        services.AddSingleton<IUpdatePowerUserRole, UpdatePowerUserRoleService>();
        services.AddSingleton<UserCreateService>();
        services.AddSingleton<IPraxisOrganizationService, PraxisOrganizationService>();
        services.AddSingleton<IPraxisFileConversionService, PraxisFileConversionService>();
        services.AddSingleton<IRiqsAdminsCreateUpdateService, RiqsAdminsCreateUpdateService>();
        services.AddSingleton<IUserCountMaintainService, UserCountMaintainService>();
        services.AddSingleton<IGenerateEquipmentMaintenanceListReport, GenerateEquipmentMaintenanceListReportService>();
        services.AddSingleton<IProcessGuideDetailReport, ProcessGuideDetailReportService>();
        services.AddSingleton<IProcessGuideCaseOverviewReport, ProcessGuideCaseOverviewReport>();
        services.AddSingleton<IGenerateDeveloperProcessGuideReport, GenerateDeveloperProcessGuideReport>();
        services.AddSingleton<IProcessGuideReportGenerateStrategy, ProcessGuideReportGenerateStrategy>();
        services.AddSingleton<GenerateProcessGuideReportForAllClient>();
        services.AddSingleton<GenerateProcessGuideReportForClientSpecific>();
        services.AddSingleton<IProvideLogoLocation, ProvideLogoLocationService>();
        services.AddSingleton<IPraxisOrganizationCreateUpdateEventService, PraxisOrganizationCreateUpdateEventService>();
        services.AddSingleton<ITasksUpdateService, TasksUpdateService>();

        #region Data Correction
        services.AddSingleton<IResolveProdDataIssuesStrategyService, ResolveProdDataIssuesStrategyService>();
        services.AddSingleton<OldDataFixService>();
        services.AddSingleton<DmsDataCorrectionService>();
        #endregion

        #region ShiftPlan Module
        services.AddSingleton<IGenerateShiftReportService, GenerateShiftReportService>();
        services.AddSingleton<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler>();
        services.AddSingleton<IPraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService, PraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService>();
        services.AddSingleton<RemoveCockpitTaskForShiftPlanSchedulerEventHandler>();
        services.AddSingleton<IRemoveCockpitTaskForShiftPlanSchedulerEventHandlerService, RemoveCockpitTaskForShiftPlanSchedulerEventHandlerService>();
        #endregion

        #region QuickTask Module
        services.AddSingleton<IQuickTaskService, QuickTaskService>();
        services.AddSingleton<IQuickTaskAssignService, QuickTaskAssignService>();
        services.AddSingleton<IQuickTaskPermissionService, QuickTaskPermissionService>();
        services.AddSingleton<IQuickTaskPlanCreatedFromSchedulerEventHandlerService, QuickTaskPlanCreatedFromSchedulerEventHandlerService>();
        services.AddSingleton<IRemoveCockpitTaskForQuickTaskPlanSchedulerEventHandlerService, RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandlerService>();

        services.AddSingleton<IGenerateQuickTaskPlanReportService, GenerateQuickTaskPlanReportService>();
        services.AddSingleton<IGenerateQuickTaskReportService, GenerateQuickTaskReportService>();
        #endregion

        #region Custom Logger
        services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
        #endregion
    }

    public static void AddPraxisEventServices(this IServiceCollection services)
    {
        // User Event
        services.AddSingleton<UserCreatedEventHandler>();
        services.AddSingleton<UserUpdatedEventHandler>();
        services.AddSingleton<UserActivatedEventHandler>();

        // Praxis User Event
        services.AddSingleton<PraxisUserEventHandler>();
        services.AddSingleton<PraxisUserCreatedEventHandler>();
        services.AddSingleton<PraxisUserUpdatedEventHandler>();

        // Organization Event
        services.AddSingleton<PraxisOrganizationEventHandler>();
        services.AddSingleton<PraxisOrganizationCreatedEventHandler>();
        services.AddSingleton<PraxisOrganizationUpdatedEventHandler>();

        // Client Event
        services.AddSingleton<PraxisClientEventHandler>();
        services.AddSingleton<PraxisClientCreatedEventHandler>();
        services.AddSingleton<PraxisClientUpdatedEventHandler>();

        // Client Category Event
        services.AddSingleton<PraxisClientCategoryEventHandler>();
        services.AddSingleton<PraxisClientCategoryCreatedEventHandler>();
        services.AddSingleton<PraxisClientCategoryUpdatedEventHandler>();

        // Praxis Form Event
        services.AddSingleton<PraxisFormEventHandler>();
        services.AddSingleton<PraxisFormCreatedEventHandler>();
        services.AddSingleton<PraxisFormUpdatedEventHandler>();

        // Praxis Task Config Event
        services.AddSingleton<PraxisTaskConfigEventHandler>();
        services.AddSingleton<PraxisTaskConfigCreatedEventHandler>();
        services.AddSingleton<PraxisTaskConfigUpdatedEventHandler>();

        // Praxis Task Event
        services.AddSingleton<PraxisTaskEventHandler>();
        services.AddSingleton<PraxisTaskCreatedEventHandler>();
        services.AddSingleton<PraxisTaskUpdatedEventHandler>();

        // Praxis Training Event
        services.AddSingleton<PraxisTrainingEventHandler>();
        services.AddSingleton<PraxisTrainingCreatedEventHandler>();
        services.AddSingleton<PraxisTrainingUpdatedEventHandler>();

        // Praxis Training Answer Event
        services.AddSingleton<PraxisTrainingAnswerEventHandler>();
        services.AddSingleton<PraxisTrainingAnswerCreatedEventHandler>();
        services.AddSingleton<PraxisTrainingAnswerUpdatedEventHandler>();
        services.AddSingleton<PraxisTrainingQualificationPassedEventHandler>();

        // Praxis Risk Management Event
        services.AddSingleton<PraxisRiskEventHandler>();
        services.AddSingleton<PraxisRiskCreatedEventHandler>();
        services.AddSingleton<PraxisRiskUpdatedEventHandler>();

        // Praxis Assessment Event
        services.AddSingleton<PraxisAssessmentEventHandler>();
        services.AddSingleton<PraxisAssessmentCreatedEventHandler>();
        services.AddSingleton<PraxisAssessmentUpdatedEventHandler>();

        // Praxis Equipment Event
        services.AddSingleton<PraxisEquipmentEventHandler>();
        services.AddSingleton<PraxisEquipmentCreatedEventHandler>();
        services.AddSingleton<PraxisEquipmentUpdatedEventHandler>();

        // Praxis Room Event
        services.AddSingleton<PraxisRoomEventHandler>();
        services.AddSingleton<PraxisRoomCreatedEventHandler>();
        services.AddSingleton<PraxisRoomUpdatedEventHandler>();

        // Praxis Equipment Maintenance Event
        services.AddSingleton<PraxisEquipmentMaintenanceEventHandler>();
        services.AddSingleton<EquipmentMaintenanceCreatedEventHandler>();
        services.AddSingleton<EquipmentMaintenanceUpdatedEventHandler>();

        // Praxis Open Item Config Event
        services.AddSingleton<PraxisOpenItemConfigEventHandler>();
        services.AddSingleton<PraxisOpenItemConfigCreatedEventHandler>();
        services.AddSingleton<PraxisOpenItemConfigUpdatedEventHandler>();

        // Praxis Open Item Event
        services.AddSingleton<PraxisOpenItemEventHandler>();
        services.AddSingleton<PraxisOpenItemCreatedEventHandler>();
        services.AddSingleton<PraxisOpenItemUpdatedEventHandler>();

        // Praxis Process Guide Event
        services.AddSingleton<PraxisProcessGuideEventHandler>();
        services.AddSingleton<PraxisProcessGuideCreateEventHandler>();
        services.AddSingleton<PraxisProcessGuideUpdateEventHandler>();

        // Task Management Event
        services.AddSingleton<TaskSummaryCreatedEventHandler>();
        services.AddSingleton<TaskScheduleCreatedEventHandler>();

        // File Conversion Event
        // services.AddSingleton<FileConversionCompletedEventHandler>();

        // Task Management Service Event
        // services.AddSingleton<TaskManagementEventHandler>();
        services.AddSingleton<TaskAssignedEventHandler>();
        services.AddSingleton<TaskOverdueEventHandler>();
        services.AddSingleton<TaskScheduleUpdateEventHandler>();

        //Praxis OpenItem CompletionInfo Event
        services.AddSingleton<PraxisOpenItemCompletionInfoEventHandler>();
        services.AddSingleton<PraxisOpenItemCompletionInfoCreatedEventHandler>();
        services.AddSingleton<PraxisOpenItemCompletionInfoUpdatedEventHandler>();

        //praxis process guide event
        services.AddSingleton<PraxisProcessGuideCreateEventHandler>();
        // services.AddSingleton<UpdateProcessGuideFromSyncEventHandler>();

        // riqs incident event
        services.AddSingleton<CirsAdminAssignedEventHandler>();

        //praxis organization create-update event
        services.AddSingleton<PraxisOrganizationCreateUpdateEventHandler>();
        services.AddSingleton<MaintenanceMailSendEventEventHandler>();
        services.AddSingleton<SubscriptionRenewEventHandler>();
        services.AddSingleton<SubscriptionExpiredEventHandler>();
        services.AddSingleton<OpenItemDeactivateEventHandler>();

        // Esignature event handler
        // services.AddSingleton<EsignatureProcessedEventHandler>();
        // services.AddSingleton<EsignatureCompleteEventHandler>();

        // praxis process guide answer event
        services.AddSingleton<PraxisProcessGuideAnswerEventHandler>();
        services.AddSingleton<PraxisProcessGuideAnswerCreatedEventHandler>();
        services.AddSingleton<PraxisProcessGuideAnswerUpdatedEventHandler>();

        // cockpit event
        services.AddSingleton<UpdateCirsAssignedAdminForCockpitEventHandler>();
        services.AddSingleton<CockpitTaskRemoveEventHandler>();

        //log event
        services.AddSingleton<AppLogRecordedEventHandler>();

        services.AddSingleton<PraxisGeneratedReportTemplatePdfEventHandler>();

        // Quick Task Event
        services.AddSingleton<QuickTaskPlanCreatedFromSchedulerEventHandler>();
        services.AddSingleton<RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler>();
    }

    public static void AddPraxisReportServices(this IServiceCollection services)
    {
        #region Reports

        services.AddSingleton<IDeveloperReportGenerateStrategy, DeveloperReportGenerateStrategyService>();
        services.AddSingleton<GenerateDeveloperReportForAllData>();
        services.AddSingleton<GenerateDeveloperReportForClientSpecific>();
        services.AddSingleton<IGenerateCategoryReport, GenerateCategoryReportService>();
        services.AddSingleton<IGenerateTrainingReport, GenerateTrainingReportService>();
        services.AddSingleton<IGenerateTrainingDetailsReport, GenerateTrainingDetailsReportService>();
        services.AddSingleton<IGenerateEquipmentReport, GenerateEquipmentReportService>();
        services.AddSingleton<IGenerateProcessMonitorOverviewReport, GenerateProcessMonitorOverviewReportService>();
        services.AddSingleton<IGenerateDistinctTaskListReport, GenerateDistinctTaskListReportService>();
        services.AddSingleton<IGenerateOpenItemReport, GenerateOpenItemReportService>();
        services.AddSingleton<IPraxisReportService, PraxisReportService>();
        services.AddSingleton<IHtmlFromTemplateGeneratorService, HtmlFromTemplateGeneratorService>();
        services.AddSingleton<IPraxisUserListReportGenerateStrategy, PraxisUserListReportGenerateStrategyService>();
        services.AddSingleton<GeneratePraxisUserListReportForAllData>();
        services.AddSingleton<GeneratePraxisUserListReportForSpecificClient>();
        services.AddSingleton<IRiskOverviewReportGenerateStrategy, RiskOverviewReportGenerateStrategyService>();
        services.AddSingleton<GenerateRiskOverviewReportForSingleClient>();
        services.AddSingleton<GenerateRiskOverviewReportForMultipleClient>();
        services.AddSingleton<IGenerateSuppliersReport, GenerateSuppliersReport>();
        services.AddSingleton<IEquipmentReportGenerationStrategyService, EquipmentReportGenerationStrategyService>();
        services.AddSingleton<GenerateEquipmentListReportForSingleClient>();
        services.AddSingleton<GenerateEquipmentListReportForMultipleClient>();
        services.AddSingleton<IGenerateLibraryDocumentAssigneesReportService, GenerateLibraryDocumentAssigneesReportService>();

        #endregion
    }

    public static void RegisterAllDerivedTypes<T>(
       this IServiceCollection services,
       Assembly[] assemblies,
       ServiceLifetime lifetime = ServiceLifetime.Singleton
   )
    {
        var typesFromAssemblies = assemblies.SelectMany(
            a => a.DefinedTypes.Where(x => x.BaseType == typeof(T) || x.GetInterfaces().Contains(typeof(T)))
        );
        foreach (var type in typesFromAssemblies)
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
    }

    public static void AddRegisterAllDerivedTypesServices(this IServiceCollection services)
    {
        services.RegisterAllDerivedTypes<IDynamicNavigationPreparation>(new[] { typeof(InsertDynamicNavigationService).Assembly });
        services.RegisterAllDerivedTypes<IEntityWiseCurrentStatus>(new[] { typeof(GetCurrentStatusForCategoryData).Assembly });
        services.RegisterAllDerivedTypes<ISupplierCurrentStatusData>(new[] { typeof(GetCurrentStatusForSupplierData).Assembly });
        services.RegisterAllDerivedTypes<IProcessUserInformation>(new[] { typeof(UserCreateService).Assembly });
        services.RegisterAllDerivedTypes<IDeleteDataByCollectionSpecific>(new[] { typeof(DeleteDataForEquipment).Assembly });
        services.RegisterAllDerivedTypes<IDeleteDataByRoleSpecific>(new[] { typeof(DeleteRiskDataForSystemAdmin).Assembly });
        services.RegisterAllDerivedTypes<IRevokePermissionForRoleSpecific>(new[] { typeof(RevokePermissionForPowerUser).Assembly });
        services.RegisterAllDerivedTypes<RevokePermissionBase>(new[] { typeof(DeleteAssessmentDataForClientAdmin).Assembly });
        services.RegisterAllDerivedTypes<IDynamicNavigationPreparation>(new[] { typeof(InsertDynamicNavigationService).Assembly });
        services.RegisterAllDerivedTypes<IDeveloperReportGenerate>(new[] { typeof(GenerateDeveloperReportForAllData).Assembly });
        services.RegisterAllDerivedTypes<IPraxisUserListReportGenerate>(new[] { typeof(GeneratePraxisUserListReportForAllData).Assembly });
        services.RegisterAllDerivedTypes<IGenerateRiskOverviewReport>(new[] { typeof(GenerateRiskOverviewReportForSingleClient).Assembly });
        services.RegisterAllDerivedTypes<IProcessUserInformation>(new[] { typeof(UserCreateService).Assembly });
        services.RegisterAllDerivedTypes<IProcessUserInformation>(new[] { typeof(ProcessNewUserDataService).Assembly });
        services.RegisterAllDerivedTypes<IProcessGuideReportGenerate>(new[] { typeof(GenerateProcessGuideReportForAllClient).Assembly });
        services.RegisterAllDerivedTypes<ICompareFileVersionFromStreamService>(new[] { typeof(ExcelFileCompareService).Assembly });
    }
}