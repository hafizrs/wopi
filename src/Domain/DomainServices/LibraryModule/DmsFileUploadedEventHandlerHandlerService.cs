using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using Microsoft.Extensions.Logging;
using Aspose.Pdf.Operators;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class DmsFileUploadedEventHandlerHandlerService : IDmsFileUploadedEventHandlerHandlerService
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactFilePermissionService _objectArtifactFilePermissionService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IObjectArtifactActivationDeactivationService _objectArtifactActivationDeactivationService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly ILibraryFileVersionComparisonService _libraryFileVersionComparisonService;
        private readonly IVectorDBFileService _vectorDBFileService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactApprovalService _objectArtifactApprovalService;
        private readonly ILogger<DmsFileUploadedEventHandlerHandlerService> _logger;

        public DmsFileUploadedEventHandlerHandlerService(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilePermissionService objectArtifactFilePermissionService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IOrganizationSubscriptionService organizationSubscriptionService,
            IObjectArtifactActivationDeactivationService objectArtifactActivationDeactivationService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            ILibraryFileVersionComparisonService libraryFileVersionComparisonService,
            IVectorDBFileService vectorDBFileService,
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactApprovalService objectArtifactApprovalService,
            ILogger<DmsFileUploadedEventHandlerHandlerService> logger
        )
        {

            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilePermissionService = objectArtifactFilePermissionService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _organizationSubscriptionService = organizationSubscriptionService;
            _objectArtifactActivationDeactivationService = objectArtifactActivationDeactivationService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _libraryFileVersionComparisonService = libraryFileVersionComparisonService;
            _vectorDBFileService = vectorDBFileService;
            _securityContextProvider = securityContextProvider;
            _objectArtifactApprovalService = objectArtifactApprovalService;
            _logger = logger;
        }

        public async Task<bool> HandleDmsFileUploadedEvent(ObjectArtifactFileUploadCommand fileUploadCommand)
        {
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(fileUploadCommand.ObjectArtifactId);

            if (objectArtifact == null) return false;

            var fileEventName = GetFileEventName(objectArtifact);

            await _vectorDBFileService.HandleManualFileUpload(objectArtifact);

            await _departmentSubscriptionService.IncrementDepartmentSubscriptionStorageUsage(objectArtifact);
            await _organizationSubscriptionService.IncrementOrganizationSubscriptionStorageUsage(objectArtifact);

            var isPermissionUpdated = await _objectArtifactFilePermissionService.SetObjectArtifactFilePermissions(objectArtifact, fileEventName);
            if (isPermissionUpdated && objectArtifact.MetaData != null && objectArtifact.MetaData.TryGetValue("ApprovalStatus", out var status) && status.Value == "1")
            {
                await _cockpitDocumentActivityMetricsGenerationService.OnDocumentUploadGenerateActivityMetrics(objectArtifact.ItemId);
            }

            await _objectArtifactUtilityService.SetObjectArtifactExtension(objectArtifact);
            await _objectArtifactUtilityService.SetMetaDataProperties(objectArtifact.ItemId);

            if (HaveOriginalArtifactIdAndUploadedFromWeb(objectArtifact))
            {
                await _objectArtifactUtilityService.CreateDocumentEditMappingRecordForExternalFiles(objectArtifact.ItemId);
                await Task.Delay(1000);
            }

            if (!fileUploadCommand.IsUploadFromInterface)
            {
                await _libraryFileVersionComparisonService.HandleLibraryFileVersionComparison(objectArtifact.ItemId);
            }

            await ApproveRiqsInterfaceArtifact(objectArtifact);

            return true;
        }

        private ObjectArtifactEvent GetFileEventName(ObjectArtifact objectArtifact)
        {
            var eventName = ObjectArtifactEvent.FILE_UPLOADED;

            if (_objectArtifactUtilityService.IsADraftedFormResponse(objectArtifact.MetaData))
            {
                eventName = ObjectArtifactEvent.FORM_RESPONSE_DRAFTED;
            }
            else if (_objectArtifactUtilityService.IsACompletedFormResponse(objectArtifact.MetaData))
            {
                eventName = ObjectArtifactEvent.FORM_RESPONSE_SAVED;
            }
            else if (_objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true))
            {
                eventName = ObjectArtifactEvent.DOCUMENT_DRAFTED;
            }
            else if (_objectArtifactUtilityService.IsASavedDraftedChildDocument(objectArtifact.MetaData))
            {
                eventName = ObjectArtifactEvent.DRAFTED_DOCUMENT_SAVED;
            }

            return eventName;
        }

        private bool HaveOriginalArtifactIdAndUploadedFromWeb(ObjectArtifact artifact)
        {
            var originalArtifactKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID)];
            var uploadedFromWebKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB)];
            
            return _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, originalArtifactKey) !=
                null && _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, uploadedFromWebKey) == ((int)LibraryBooleanEnum.TRUE).ToString();
        }

        private async Task ApproveRiqsInterfaceArtifact(ObjectArtifact artifact)
        {
            try
            {
                var isAInterfaceMigrationArtifact = _objectArtifactUtilityService.IsAInterfaceMigrationArtifact(artifact?.MetaData);

                if (isAInterfaceMigrationArtifact)
                {
                    var command = new ObjectArtifactApprovalCommand
                    {
                        ObjectArtifactId = artifact.ItemId
                    };
                    _logger.LogInformation("Auto Approve Riqs Interface Artifact: {id}", artifact.ItemId);
                    await _objectArtifactApprovalService.InitiateObjectArtifactApprovalProcess(command);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }
    }
}
