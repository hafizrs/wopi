using Aspose.Pdf;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Globalization;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DocumentEditMappingService : IDocumentEditMappingService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<DocumentEditMappingService> _logger;
        private readonly IDocGenerationService _docGenerationService;
        private readonly IPraxisFileService _fileService;
        private readonly IStorageDataService _storageDataService;
        private readonly IDmsService _dmsService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly ILibraryStandardDocumentService _libraryStandardDocumentService;
        public DocumentEditMappingService
        (
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<DocumentEditMappingService> logger,
            IDocGenerationService docGenerationService,
            IPraxisFileService fileService,
            IStorageDataService storageDataService,
            IDmsService dmsService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ISecurityHelperService securityHelperService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            ILibraryStandardDocumentService libraryStandardDocumentService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
            _docGenerationService = docGenerationService;
            _fileService = fileService;
            _storageDataService = storageDataService;
            _dmsService = dmsService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _securityHelperService = securityHelperService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _libraryStandardDocumentService = libraryStandardDocumentService;
        }

        public async Task<bool> IsAValidArtifactEditRequest(string artifactId, string fileType, CommandResponse response = null)
        {
            if (fileType == ((int)LibraryFileTypeEnum.DOCUMENT).ToString())
            {
                var isValid = await IsAValidArtifactEditRequestForDocumentType(artifactId);
                if (!isValid && response != null)
                {
                    response.SetError("command", "This document has an existing draft");
                }
                return isValid;
            }
            else
            {
                if (response != null) response.SetError("command", "Invalid FileType");
            }
            return false;
        }

        private async Task<bool> IsAValidArtifactEditRequestForDocumentType(string artifactId)
        {
            var childArtifactDocMap = await GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(artifactId);
            return !(childArtifactDocMap != null && childArtifactDocMap.IsDraft && !string.IsNullOrEmpty(childArtifactDocMap.ObjectArtifactId));
        }

        public async Task CreateDocumentEditMappingRecord(ObjectArtifact objectArtifactData, string newHtmlFileId, string originalHtmlFileId, SecurityContext securityContext)
        {
            try
            {
                var existingDocumentMappingData = await GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(objectArtifactData.ItemId);

                if (existingDocumentMappingData == null)
                {
                    var createdDocumentEditRecord = new DocumentEditMappingRecord()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow,
                        CreatedBy = securityContext.UserId,
                        IsDraft = true,
                        ParentObjectArtifactId = objectArtifactData.ItemId,
                        OriginalHtmlFileId = originalHtmlFileId,
                        CurrentHtmlFileId = newHtmlFileId,
                        EditHistory = new List<DocumentEditRecordHistory>(),
                        DepartmentId = _securityHelperService.IsADepartmentLevelUser() ? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty,
                        OrganizationId = objectArtifactData?.OrganizationId
                    };

                    await _repository.SaveAsync(createdDocumentEditRecord);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in CreateDocumentEditMappingRecord -> Message: {ex.Message} Exception Details: {ex.StackTrace}");
            }
        }

        public async Task<DocumentEditMappingRecord> GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(string objectArtifactId)
        {
            Expression<Func<DocumentEditMappingRecord, bool>> filter = m => m.IsDraft && !m.IsMarkedToDelete &&
                    m.ParentObjectArtifactId == objectArtifactId;
            if (_securityHelperService.IsADepartmentLevelUser())
            {
                var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                filter = m => m.IsDraft && !m.IsMarkedToDelete &&
                    m.ParentObjectArtifactId == objectArtifactId && m.DepartmentId == departmentId;
            }
            else if (_securityHelperService.IsAAdminBUser())
            {
                filter = m => m.IsDraft && !m.IsMarkedToDelete &&
                    m.ParentObjectArtifactId == objectArtifactId && string.IsNullOrEmpty(m.DepartmentId);
            }
            return await _repository.GetItemAsync(filter);
        }

        public async Task<DocumentEditMappingRecord> GetDocumentMappingDraftHtmlFileInfo(string objectArtifactId)
        {
            return await _repository.GetItemAsync<DocumentEditMappingRecord>(m => m.IsDraft && !m.IsMarkedToDelete &&
                                                                                m.ObjectArtifactId == objectArtifactId
                                                                            );
        }

        public async Task SaveObjectArtifactDocumentDraftedData(DmsFileUploadPayload dmsUploadPayload,
            string parentObjectArtifactId)
        {
            try
            {
                _ = await DraftDocumentEditRecord(parentObjectArtifactId);
                var securityContext = _securityContextProvider.GetSecurityContext();
                var draftedData = await GetDocumentEditMappingRecordByDraftArtifact(parentObjectArtifactId);
                var metaData = dmsUploadPayload.MetaData ?? new Dictionary<string, MetaValuePair>();
                var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.VERSION.ToString()];
                var artifact =
                    await _repository.GetItemAsync<ObjectArtifact>(o =>
                        o.FileStorageId == dmsUploadPayload.FileStorageId);
                if (draftedData != null && artifact != null)
                {
                    draftedData.ObjectArtifactId = artifact.ItemId;
                    draftedData.Version = metaData.TryGetValue(versionKey, out MetaValuePair version)
                        ? version.Value
                        : null;
                    draftedData.ArtifactVersionCreateDate = DateTime.UtcNow;
                    draftedData.IsDraft = false;
                    draftedData.SavedDocUserId = securityContext.UserId;
                    draftedData.SavedDocUserDisplayName = securityContext.DisplayName;
                    await _repository.UpdateAsync(d => d.ItemId == draftedData.ItemId, draftedData);
                    await UpdateDocumentEditMetaData(draftedData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in SaveObjectArtifactDocumentDraftedData -> message: {ex.Message} Exception Details: {ex.StackTrace}");
            }
        }

        public async Task UpdateDocumentEditMetaData(DocumentEditMappingRecord draftedData)
        {
            if (string.IsNullOrEmpty(draftedData?.ObjectArtifactId)) return;
            var editedDate = draftedData?.EditHistory?.OrderByDescending(e => e.EditDate)?.FirstOrDefault()?.EditDate;
            var artifact = await _repository.GetItemAsync<ObjectArtifact>(o => o.ItemId == draftedData.ObjectArtifactId);
            if (artifact?.MetaData != null && editedDate != null)
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

                await _repository.UpdateAsync(e => e.ItemId == artifact.ItemId, artifact);
            }
        }

        public async Task<bool> DraftDocumentEditRecord(string objectArtifactId)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var documentEditRecord = await GetDocumentEditMappingRecordByDraftArtifact(objectArtifactId);

                if (documentEditRecord == null)
                {
                    _logger.LogInformation(
                        $" No object artifact found to  DraftDocumentEditRecord  ObjectartifactId : {objectArtifactId}");
                    return await Task.FromResult(false);
                }

                var editHistory = documentEditRecord.EditHistory.Find(x => x.EditorUserId == securityContext.UserId);
                if (editHistory != null)
                {
                    editHistory.EditDate = DateTime.UtcNow;
                    await _repository.UpdateAsync(d => d.ItemId == documentEditRecord.ItemId, documentEditRecord);
                }
                else
                {
                    var editHistoryData = new DocumentEditRecordHistory()
                    {
                        EditorUserId = securityContext.UserId,
                        EditorDisplayName = securityContext.DisplayName,
                        EditDate = DateTime.UtcNow
                    };

                    if (documentEditRecord.EditHistory == null)
                    {
                        documentEditRecord.EditHistory = new List<DocumentEditRecordHistory>()
                        {
                            editHistoryData
                        };
                    }
                    else
                    {
                        documentEditRecord.EditHistory.Add(editHistoryData);
                    }

                    await _repository.UpdateAsync(d => d.ItemId == documentEditRecord.ItemId, documentEditRecord);
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in DraftDocumentEditRecord -> message: {ex.Message} Exception Details: {ex.StackTrace}");

                return await Task.FromResult(false);
            }
        }

        public async Task<bool> IsVersionHistoryAvailable(string objectArtifactId, List<DocumentEditMappingRecord> documentMappingDatas = null)
        {
            try
            {
                if (documentMappingDatas != null)
                {
                    var mappingData = documentMappingDatas?.Find(d => d.ObjectArtifactId == objectArtifactId && !d.IsDraft);
                    await Task.FromResult(mappingData != null);
                }
                return await _repository.ExistsAsync<DocumentEditMappingRecord>(m =>
                    !m.IsDraft && m.ObjectArtifactId == objectArtifactId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in IsVersionHistoryAvaialble -> message: {ex.Message} Exception Details: {ex.StackTrace}");

                return await Task.FromResult(false);
            }
        }
        private async Task<Stream> GetOriginalDocStream(string objectArtifactId)
        {
            var objectArtifact = await _repository.GetItemAsync<ObjectArtifact>(o =>
                           o.ItemId == objectArtifactId);
            if (objectArtifact == null)
            {
                throw new ArgumentException("Invalid object artifact ID");
            }

            var file = await _fileService.GetFileInfoFromStorage(objectArtifact.FileStorageId);
            Stream htmlStream = _storageDataService.GetFileContentStream(file.Url);
            return htmlStream;
        }
        public async Task<bool> ProcessDocumentEditHtmlDocument(string objectArtifactId)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var documentMappingData = await GetDocumentEditMappingRecordByDraftArtifact(objectArtifactId);
                var isUploaded = false;
                var objectArtifact = await _repository.GetItemAsync<ObjectArtifact>(o => o.ItemId == objectArtifactId);
                var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE}"];
                if (objectArtifact != null && _objectArtifactUtilityService.GetMetaDataValueByKey(
                        objectArtifact.MetaData,
                        isChildStandardFileKey) == "1" &&
                    !_objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true))
                {
                    documentMappingData = _repository.GetItem<DocumentEditMappingRecord>(d =>
                        d.ObjectArtifactId == objectArtifactId);
                    if (documentMappingData != null)
                    {
                        var file = await _fileService.GetFileInfoFromStorage(documentMappingData.CurrentHtmlFileId);
                        if (file != null)
                        {

                            Stream htmlStream = _storageDataService.GetFileContentStream(file.Url);
                            _logger.LogInformation("html stream completed");
                            var docByteData =
                                _docGenerationService.PrepareObjectArtifactDocumentFromHtmlStream(htmlStream);

                            _logger.LogInformation("html to doc generation completed");
                            htmlStream.Close();
                            htmlStream.Dispose();
                            if (docByteData != null && docByteData.Length > 0)
                            {
                                isUploaded = await _storageDataService.UploadFileAsync(objectArtifact.FileStorageId, objectArtifact.Name, docByteData, objectArtifact.Tags);

                                _logger.LogInformation("html to doc upload completed");
                            }
                        }

                        documentMappingData.CurrentDocFileId = objectArtifact.FileStorageId;
                        await _libraryStandardDocumentService.UpdateDocumentEditRecordHistory(documentMappingData);
                        await UpdateDocumentEditMetaData(documentMappingData);
                    }
                }
                else if (documentMappingData != null)
                {
                   
                    var file = await _fileService.GetFileInfoFromStorage(documentMappingData.CurrentHtmlFileId);
                    if (file != null)
                    {
                       
                        Stream htmlStream = _storageDataService.GetFileContentStream(file.Url);
                        var docByteData = _docGenerationService.PrepareObjectArtifactDocumentFromHtmlStream(htmlStream);
                        htmlStream.Close();
                        htmlStream.Dispose();
                        
                        var artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId);
                        if (docByteData != null && docByteData.Length > 0)
                        {
                            var fileId = documentMappingData.CurrentDocFileId ?? Guid.NewGuid().ToString();
                            if (string.IsNullOrEmpty(documentMappingData.ObjectArtifactId))
                            {
                                var clonedArtifactId = await DuplicateFromParentObjectArtifact(artifact, fileId, docByteData);
                                documentMappingData.ObjectArtifactId = clonedArtifactId;
                                isUploaded = !string.IsNullOrEmpty(clonedArtifactId);
                            }
                            else
                            {
                                isUploaded = await _storageDataService.UploadFileAsync(fileId, artifact.Name, docByteData, artifact.Tags);
                            }

                            if (isUploaded)
                            {
                                documentMappingData.CurrentDocFileId = fileId;
                                await _repository.UpdateAsync(d => d.ItemId == documentMappingData.ItemId,
                                    documentMappingData);

                                await DraftDocumentEditRecord(objectArtifactId);
                                await UpdateDocumentEditMetaData(documentMappingData);
                                // await _cockpitDocumentActivityMetricsGenerationService.OnDocFileEditGenerateActivityMetrics(new string[] { artifact.ItemId });
                            }
                        }
                    }
                }

                return isUploaded;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured in ProcessObjectArtifactHtmlDocument: {ex.Message} -> {ex.StackTrace}");
                return false;
            }
        }

        private async Task<string> DuplicateFromParentObjectArtifact(ObjectArtifact artifact, string fileId, byte[] docByteData)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var metaData = _dmsService.PrepareMetaDataForDmsDocumentFileUpload(artifact, false, true);
            var userId = securityContext.UserId;
            var workSpace = GetWorkSpaceForCurrentUserId(userId, artifact.OwnerId);
            var dmsUploadPayload = new ObjectArtifactFileUploadCommand()
            {
                FileStorageId = fileId,
                Description = null,
                Tags = artifact.Tags,
                ParentId = artifact.ParentId,
                FileName = artifact.Name,
                StorageAreaId = artifact.StorageAreaId,
                ObjectArtifactId = Guid.NewGuid().ToString(),
                WorkspaceId = workSpace?.ItemId,
                UserId = userId,
                OrganizationId = artifact.OrganizationId,
                UseLicensing = true,
                FileSizeInBytes = docByteData.Length,
                FeatureId = "praxis-license",
                MetaData = metaData
            };

            var uploadUrl = await _dmsService.UploadFile(dmsUploadPayload, securityContext.OauthBearerToken);
            if (!string.IsNullOrEmpty(uploadUrl))
            {
                var isUploaded = await _storageDataService.UploadFileToStorageByUrlAsync(uploadUrl, docByteData);
                if (isUploaded)
                {
                    return dmsUploadPayload.ObjectArtifactId;
                }
            }
            return string.Empty;
        }

        private Workspace GetWorkSpaceForCurrentUserId(string userId, string ownerId)
        {

            var workspaces = _repository.GetItems<Workspace>(w => w.OwnerId == userId)?.ToList();
            try
            {
                if (workspaces == null || workspaces?.Count == 0)
                {
                    workspaces = _repository.GetItems<Workspace>(w => w.OwnerId == ownerId)?.ToList();
                    foreach (var workspace in workspaces)
                    {
                        workspace.ItemId = Guid.NewGuid().ToString();
                        workspace.OwnerId = userId;
                        workspace.CreatedBy = userId;
                        workspace.IdsAllowedToRead = new string[] { userId };
                        workspace.IdsAllowedToWrite = new string[] { userId };
                        _repository.Save(workspace);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in GetWorkSpaceForCurrentUserId {ex.Message}");
            }
            return workspaces?.Find(w => w.Name == "My Workspace");
        }

        public async Task<DocumentEditMappingRecord> GetDocumentEditMappingRecordByDraftArtifact(string objectArtifactId)
        {
            var artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId);
            if (artifact != null && _objectArtifactUtilityService.IsADocument(artifact?.MetaData, true))
            {
                return await GetDocumentMappingDraftHtmlFileInfo(objectArtifactId);
            }
            else if (artifact != null)
            {
                return await GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(objectArtifactId);
            }
            return null;
        }
    }
}
