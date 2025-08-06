using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class DmsService : IDmsService
    {
        private readonly ILogger<DmsService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactDeleteService _objectArtifactDeleteService;
        private readonly IObjectArtifactService _objectArtifactService;
        private readonly IObjectArifactVersionService _objectArifactVersionService;
        public DmsService(ILogger<DmsService> logger,
            IRepository repository,
            ISecurityHelperService securityHelperService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactDeleteService objectArtifactDeleteService,
            IObjectArtifactService objectArtifactService,
            IObjectArifactVersionService objectArifactVersionService
        )
        {
            _logger = logger;
            _repository = repository;
            _securityHelperService = securityHelperService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactDeleteService = objectArtifactDeleteService;
            _objectArtifactService = objectArtifactService;
            _objectArifactVersionService = objectArifactVersionService;
        }

        public async Task<bool> CreateFolder(ObjectArtifactFolderCreateCommand payload)
        {
            try
            {
                var response = await _objectArtifactService.InitiateObjectArtifactFolderCreateAsync(payload);
                return response.StatusCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in DmsService in CreateFolder with error -> {ErrorMessage}, trace -> {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<List<ObjectArtifactFolderCreateCommand>> CreateFolders(List<ObjectArtifactFolderCreateCommand> payloadList)
        {

            try
            {
                var successfulPayloads = await _objectArtifactService.InitiateObjectArtifactFolderListCreateAsync(payloadList);
                _logger.LogInformation("{SuccessCount} folders succeeded to create.", successfulPayloads.Count);

                int failedCount = payloadList.Count - successfulPayloads.Count;

                if (failedCount > 0)
                {
                    _logger.LogWarning("{FailedCount} folders failed to create.", failedCount);
                }

                return successfulPayloads;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in DmsService in CreateFolders with error -> {ErrorMessage}, trace -> {StackTrace}", ex.Message, ex.StackTrace);
                return new List<ObjectArtifactFolderCreateCommand>();
            }
        }

        public async Task<string> UploadFile(ObjectArtifactFileUploadCommand payload, string token)
        {
            try
            {
                var isValid = await CheckValidUploadFileRequest(payload);

                if (!isValid)
                {
                    _logger.LogWarning("Storage limit exceeded for department subscription. File size: {FileSizeInBytes}", payload.FileSizeInBytes);
                    return string.Empty;
                }

                var response = await _objectArtifactService.InitiateObjectArtifactFileUploadAsync(payload);
                _logger.LogInformation("response: -> {Response}", JsonConvert.SerializeObject(response));

                if (response != null && response.StatusCode == 0 && response.RequestUri != null)
                {
                    return response.RequestUri;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in DmsService in ShareFolder with error -> {ErrorMessage}, trace -> {StackTrace}", ex.Message, ex.StackTrace);
            }
            return string.Empty;
        }

        public async Task<bool> DeleteObjectArtifact(string objectArtifactId, string organizationId = "")
        {
            try
            {
                var isDeleted = await _objectArtifactDeleteService.DeleteObjectArtifact(objectArtifactId);
                return isDeleted?.StatusCode == 0 && isDeleted.Errors?.IsValid == true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in DmsService in DeleteFolder with error -> {ErrorMessage}, trace -> {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public IDictionary<string, MetaValuePair> PrepareMetaDataForDmsDocumentFileUpload(ObjectArtifact objectArtifact, bool alreadyDrafted, bool saveAsDraft, bool isNotifiedToCockpit = false)
        {
            var metaData = objectArtifact?.MetaData;
            var newMetaData = new Dictionary<string, MetaValuePair>();

            var isDraftKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_DRAFT.ToString()];
            var statusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];
            var approvalStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS.ToString()];
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID.ToString()];
            var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.VERSION.ToString()];
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];
            var isOrgLevelKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL.ToString()];
            var isStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE.ToString()];
            var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE.ToString()];
            var isNotifiedToCockpitKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_NOTIFIED_TO_COCKPIT.ToString()];

            var removeMetaDataKeys = new List<string>() {
                isDraftKey,
                departmentIdKey,
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON.ToString()],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE.ToString()],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START_DATE.ToString()],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT)],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB)],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID_FOR_SUBSCRIPTION)],
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID.ToString()],
                isOrgLevelKey,
                isStandardFileKey,
                isNotifiedToCockpitKey
            };
            var standardFile = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifact?.MetaData, isStandardFileKey);
            if (metaData != null)
            {
                foreach (var data in metaData)
                {
                    newMetaData.Add(data.Key, data.Value);
                }

                if (newMetaData.TryGetValue(versionKey, out MetaValuePair version) && !alreadyDrafted)
                {
                    version.Value = GenerateVersionFromParentObjectArtifact(version.Value);
                }

                foreach (var key in removeMetaDataKeys)
                {
                    if (newMetaData.ContainsKey(key))
                    {
                        newMetaData.Remove(key);
                    }
                }
            }

            var resetMetaData = new Dictionary<string, MetaValuePair>()
            {
                { statusKey, new MetaValuePair() { Type = "string", Value = ((int)LibraryFileStatusEnum.INACTIVE).ToString() } },
                { approvalStatusKey,  new MetaValuePair() { Type = "string", Value = ((int)LibraryFileApprovalStatusEnum.PENDING).ToString() } }
            };

            if (!alreadyDrafted)
            {
                resetMetaData.Add(originalArtifactIdKey, new MetaValuePair() { Type = "string", Value = objectArtifact.ItemId });
            }
            if (saveAsDraft)
            {
                resetMetaData.Add(isDraftKey, new MetaValuePair() { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() });
            }

            if (standardFile == "1")
            {
                resetMetaData.Add(isChildStandardFileKey, new MetaValuePair() { Type = "string", Value = "1" });
            }
            if (_objectArtifactUtilityService.IsAOrgLevelArtifact(newMetaData, objectArtifact.ArtifactType))
            {
                resetMetaData.Add(isOrgLevelKey, new MetaValuePair() { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() });
            }

            if (_securityHelperService.IsADepartmentLevelUser() && !_objectArtifactUtilityService.IsAOrgLevelArtifact(newMetaData, objectArtifact.ArtifactType))
            {
                var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                if (!string.IsNullOrEmpty(departmentId))
                {
                    resetMetaData.Add(departmentIdKey, new MetaValuePair() { Type = "string", Value = departmentId });
                }
            }
            if (isNotifiedToCockpit)
            {
                resetMetaData.Add(isNotifiedToCockpitKey, new MetaValuePair() { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() });
            }

            foreach (var data in resetMetaData)
            {
                if (newMetaData.TryGetValue(data.Key, out MetaValuePair _))
                {
                    newMetaData[data.Key] = data.Value;
                }
                else
                {
                    newMetaData.Add(data.Key, data.Value);
                }
            }

            return newMetaData;
        }

        public string GenerateVersionFromParentObjectArtifact(string parentVersion)
        {
            try
            {
                if (string.IsNullOrEmpty(parentVersion)) parentVersion = "1";
                var childVersion = "0";

                var parentVersionPos = parentVersion.LastIndexOf(".", StringComparison.Ordinal);
                if (parentVersionPos >= 0)
                {
                    if (parentVersionPos + 1 < parentVersion.Length)
                    {
                        childVersion = parentVersion.Substring(parentVersionPos + 1) ?? "0";
                    }

                    parentVersion = parentVersion.Substring(0, parentVersionPos);
                }
                var isLibraryAdmin = _objectArifactVersionService.GenerateParentVersionWithLibraryAdminIfParentArtifactIsNotEmpty();
                if (!isLibraryAdmin)
                {
                    childVersion = (int.Parse(childVersion) + 1).ToString();
                }
                else
                {
                    parentVersion = (int.Parse(parentVersion) + 1).ToString();
                    childVersion = "0";
                }
                childVersion = (int.Parse(childVersion) < 10 ? "0" : "") + childVersion;
                return parentVersion + "." + childVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in GenerateVersionFromParentObjectArtifact -> message: {ErrorMessage} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        public async Task DeleteObjectArtifactsAsync(List<string> fileIds)
        {
            var objectArtifacts = _repository.GetItems<ObjectArtifact>(artifact => fileIds.Contains(artifact.FileStorageId));

            if (objectArtifacts != null)
            {
                foreach (var objectArtifact in objectArtifacts)
                {
                    await DeleteObjectArtifact(objectArtifact.ItemId, objectArtifact.OrganizationId);
                }
            }
        }

        private async Task<bool> CheckValidUploadFileRequest(DmsFileUploadPayload payload)
        {
            if (payload.MetaData == null) return false;

            var departmentIdForSubscriptionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID_FOR_SUBSCRIPTION.ToString()];
            var departmentIdForSubscription = _objectArtifactUtilityService.GetMetaDataValueByKey(payload.MetaData, departmentIdForSubscriptionKey);

            var requestQuery = new GetValidFileUploadRequestQuery
            {
                PraxisClientId = departmentIdForSubscription,
                FileSizeInBytes = payload.FileSizeInBytes
            };

            var validUploadFileRequest = await _departmentSubscriptionService.GetValidUploadFileRequestInDepartmentSubscription(requestQuery);

            if (!string.IsNullOrEmpty(validUploadFileRequest.PraxisClientId))
            {
                if (payload.MetaData.TryGetValue(departmentIdForSubscriptionKey, out MetaValuePair _))
                {
                    payload.MetaData[departmentIdForSubscriptionKey].Value = validUploadFileRequest.PraxisClientId;
                }
                else
                {
                    payload.MetaData.Add(departmentIdForSubscriptionKey, new MetaValuePair() { Type = "string", Value = validUploadFileRequest.PraxisClientId });
                }
            }

            return validUploadFileRequest.IsValid;
        }

    }
}
