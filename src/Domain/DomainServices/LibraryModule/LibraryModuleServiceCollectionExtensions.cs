using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Domain;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Utils;

public static class LibraryModuleServiceCollectionExtensions
{
    public static void AddLibraryModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IObjectArtifactValidationService, ObjectArtifactValidationService>();
        services.AddSingleton<IAssignLibraryAdminsService, AssignLibraryAdminsService>();
        services.AddSingleton<IPraxisOrganizationUserService, PraxisOrganizationUserService>();
        services.AddSingleton<IObjectArtifactSearchService, ObjectArtifactSearchService>();
        services.AddSingleton<IObjectArtifactSearchQueryBuilderService, ObjectArtifactSearchQueryBuilderService>();
        services.AddSingleton<IObjectArtifactSearchResponseGeneratorService, ObjectArtifactSearchResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactUpdateService, ObjectArtifactUpdateService>();
        services.AddSingleton<IObjectArtifactService, ObjectArtifactService>();
        services.AddSingleton<IObjectArtifactActivationDeactivationService, ObjectArtifactActivationDeactivationService>();
        services.AddSingleton<IObjectArtifactApprovalService, ObjectArtifactApprovalService>();
        services.AddSingleton<ICreateLibraryGroupService, CreateLibraryGroupService>();
        services.AddSingleton<IObjectArtifactAuthorizationCheckerService, ObjectArtifactAuthorizationCheckerService>();
        services.AddSingleton<IObjectArtifactPermissionGeneratorService, ObjectArtifactPermissionGeneratorService>();
        services.AddSingleton<IGetLibraryGroupsService, GetLibraryGroupsService>();
        services.AddSingleton<IObjectArtifactFileConversionService, ObjectArtifactFileConversionService>();
        services.AddSingleton<IDocumentEditMappingService, DocumentEditMappingService>();
        services.AddSingleton<IDocGenerationService, DocGenerationService>();
        services.AddSingleton<IObjectArtifactUtilityService, ObjectArtifactUtilityService>();
        services.AddSingleton<IObjectArtifactFilePermissionService, ObjectArtifactFilePermissionService>();
        services.AddSingleton<IObjectArtifactFolderPermissionService, ObjectArtifactFolderPermissionService>();
        services.AddSingleton<IObjectArtifactPermissionHelperService, ObjectArtifactPermissionHelperService>();
        services.AddSingleton<IObjectArtifactSharedDataResponseGeneratorService, ObjectArtifactSharedDataResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactReportsSharedDataResponseGeneratorService, ObjectArtifactReportsSharedDataResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactFolderShareService, ObjectArtifactFolderShareService>();
        services.AddSingleton<IObjectArtifactFileShareService, ObjectArtifactFileShareService>();
        services.AddSingleton<IObjectArtifactShareService, ObjectArtifactShareService>();
        services.AddSingleton<IObjectArtifactSyncService, ObjectArtifactSyncService>();
        services.AddSingleton<IDocumentKeywordService, DocumentKeywordService>();
        services.AddSingleton<ILibraryDirectoryGetService, LibraryDirectoryGetService>();
        services.AddSingleton<IObjectArtifactSearchUtilityService, ObjectArtifactSearchUtilityService>();
        services.AddSingleton<IObjectArtifactFilterUtilityService, ObjectArtifactFilterUtilityService>();
        services.AddSingleton<IObjectArtifactFormHistoryService, ObjectArtifactFormHistoryService>();
        services.AddSingleton<IObjectArtifactMoveService, ObjectArtifactMoveService>();
        services.AddSingleton<ILibraryFormService, LibraryFormService>();
        services.AddSingleton<ILibraryFormAdoptFactoryService, LibraryFormAdoptFactoryService>();
        services.AddSingleton<IObjectArtifactDeleteService, ObjectArtifactDeleteService>();
        services.AddSingleton<ISignatureService, SignatureService>();
        services.AddSingleton<StorageServiceFactory>();
        services.AddSingleton<PraxisFileServiceFactory>();
        services.AddSingleton<IObjectArtifactMappingService, ObjectArtifactMappingService>();
        services.AddSingleton<ILibraryDocumentAssigneeService, LibraryDocumentAssigneeService>();
        services.AddSingleton<IObjectArtifactQueryService, ObjectArtifactQueryService>();
        services.AddSingleton<IObjectArtifactFileQueryService, ObjectArtifactFileQueryService>();
        services.AddSingleton<IDmsArtifactUsageReferenceQueryService, DmsArtifactUsageReferenceQueryService>();
        services.AddSingleton<IGenericEventPublishService, GenericEventPublishService>();
        services.AddSingleton<ILibraryDocumentAssigneeService, LibraryDocumentAssigneeService>();
        services.AddSingleton<IVectorDBFileService, VectorDBFileService>();
        services.AddSingleton<IItemsUsageInEntitiesQueryService, ItemsUsageInEntitiesQueryService>();
        services.AddSingleton<ILibraryStandardDocumentService, LibraryStandardDocumentService>();
        services.AddSingleton<IRiqsPediaViewControlService, RiqsPediaViewControlService>();
        services.AddSingleton<IRiqsPediaQueryService, RiqsPediaQueryService>();
        services.AddSingleton<IObjectArifactVersionService, ObjectArifactVersionService>();
        services.AddSingleton<IDmsFolderCreatedEventHandlerHandlerService, DmsFolderCreatedEventHandlerHandlerService>();
        services.AddSingleton<IDmsFileUploadedEventHandlerHandlerService, DmsFileUploadedEventHandlerHandlerService>();
        services.AddSingleton<IObjectArtifactSyncService, ObjectArtifactSyncService>();
        services.AddSingleton<ILibraryFolderSharedEventHandlerService, LibraryFolderSharedEventHandlerService>();

        // DMS event handlers
        services.AddSingleton<DmsFileUploadedEventHandler>();
        services.AddSingleton<DmsFolderCreatedEventHandler>();

        // Library event handlers
        services.AddSingleton<LibraryFileSharedEventHandler>();
        services.AddSingleton<LibraryFolderSharedEventHandler>();
        services.AddSingleton<LibraryFolderTreeSharedEventHandler>();
        services.AddSingleton<LibraryFormUpdateEventHandler>();
        services.AddSingleton<LibraryRightsUpdatedEventHandler>();
        services.AddSingleton<LibraryFileApprovedEventHandler>();
        services.AddSingleton<LibraryFileRenamedEventHandler>();
        services.AddSingleton<LibraryFileMovedEventHandler>();
        services.AddSingleton<LibraryFileDeletedEventHandler>();
        services.AddSingleton<DmsArtifactReapprovalEventHandler>();
        services.AddSingleton<LibraryFileEditedByOthersEventHandler>();
        services.AddSingleton<DmsArtifactUsageReferenceEventHandler>();
        services.AddSingleton<DmsArtifactUsageReferenceDeletionEventHandler>();

        // Event handler services
        services.AddSingleton<IDmsFolderCreatedEventHandlerHandlerService, DmsFolderCreatedEventHandlerHandlerService>();
        services.AddSingleton<IDmsFileUploadedEventHandlerHandlerService, DmsFileUploadedEventHandlerHandlerService>();
        services.AddSingleton<ILibraryFileSharedEventHandlerService, LibraryFileSharedEventHandlerService>();
        services.AddSingleton<ILibraryFolderSharedEventHandlerService, LibraryFolderSharedEventHandlerService>();
        services.AddSingleton<ILibraryFolderTreeSharedEventHandlerService, LibraryFolderTreeSharedEventHandlerService>();
        services.AddSingleton<ILibraryFormUpdateEventHandlerService, LibraryFormUpdateEventHandlerService>();
        services.AddSingleton<ILibraryRightsUpdatedEventHandlerService, LibraryRightsUpdatedEventHandlerService>();
        services.AddSingleton<ILibraryFileApprovedEventHandlerService, LibraryFileApprovedEventHandlerService>();
        services.AddSingleton<ILibraryFileRenamedEventHandlerService, LibraryFileRenamedEventHandlerService>();
        services.AddSingleton<ILibraryFileMovedEventHandlerService, LibraryFileMovedEventHandlerService>();
        services.AddSingleton<ILibraryFileDeletedEventHandlerService, LibraryFileDeletedEventHandlerService>();
        services.AddSingleton<IDmsArtifactReapprovalEventHandlerService, DmsArtifactReapprovalEventHandlerService>();
        services.AddSingleton<ILibraryFileEditedByOthersEventHandlerService, LibraryFileEditedByOthersEventHandlerService>();
        services.AddSingleton<IDmsArtifactUsageReferenceEventHandlerService, DmsArtifactUsageReferenceEventHandlerService>();

        // Domain Services
        services.AddSingleton<IObjectArtifactFilePermissionService, ObjectArtifactFilePermissionService>();
        services.AddSingleton<IObjectArtifactFolderPermissionService, ObjectArtifactFolderPermissionService>();
        services.AddSingleton<IObjectArtifactPermissionHelperService, ObjectArtifactPermissionHelperService>();
        services.AddSingleton<IObjectArtifactPermissionGeneratorService, ObjectArtifactPermissionGeneratorService>();
        services.AddSingleton<IObjectArtifactSearchService, ObjectArtifactSearchService>();
        services.AddSingleton<IObjectArtifactSearchQueryBuilderService, ObjectArtifactSearchQueryBuilderService>();
        services.AddSingleton<IObjectArtifactSearchResponseGeneratorService, ObjectArtifactSearchResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactSharedDataResponseGeneratorService, ObjectArtifactSharedDataResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactReportsSharedDataResponseGeneratorService, ObjectArtifactReportsSharedDataResponseGeneratorService>();
        services.AddSingleton<IObjectArtifactShareService, ObjectArtifactShareService>();
        services.AddSingleton<IDocumentKeywordService, DocumentKeywordService>();
        services.AddSingleton<IObjectArtifactUtilityService, ObjectArtifactUtilityService>();
        services.AddSingleton<IObjectArtifactSearchUtilityService, ObjectArtifactSearchUtilityService>();
        services.AddSingleton<IObjectArtifactFilterUtilityService, ObjectArtifactFilterUtilityService>();
        services.AddSingleton<IObjectArtifactAuthorizationCheckerService, ObjectArtifactAuthorizationCheckerService>();
        services.AddSingleton<IObjectArtifactDeleteService, ObjectArtifactDeleteService>();
        services.AddSingleton<IObjectArtifactFileQueryService, ObjectArtifactFileQueryService>();
        services.AddSingleton<IRiqsPediaViewControlService, RiqsPediaViewControlService>();
        // Document services
        services.AddSingleton<IDocGenerationService, DocGenerationService>();
        services.AddSingleton<IObjectArtifactFileConversionService, ObjectArtifactFileConversionService>();
        services.AddSingleton<IDocumentEditMappingService, DocumentEditMappingService>();
        services.AddSingleton<IDocumentEditHistoryService, DocumentEditHistoryService>();
        // Form services

        services.AddSingleton<ILibraryFormService, LibraryFormService>();
        services.AddSingleton<StorageServiceFactory>();
        services.AddSingleton<PraxisFileServiceFactory>();
        services.AddSingleton<ISignatureService, SignatureService>();
        services.AddSingleton<ITokenService, TokenService>();

        // Report services
        services.AddSingleton<IGenerateLibraryReportService, GenerateLibraryReportService>();

        services.AddSingleton<ICompareLibraryFileVersionFactoryService, CompareLibraryFileVersionFactoryService>();
        services.AddSingleton<ILibraryFileVersionComparisonService, LibraryFileVersionComparisonService>();
        services.AddSingleton<ExcelFileCompareService>();
        services.AddSingleton<DocumentFileCompareService>();
        services.AddSingleton<PdfFileCompareService>();
    }
}
