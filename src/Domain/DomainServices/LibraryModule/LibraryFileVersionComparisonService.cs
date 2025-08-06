using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Text.Json.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using File = Selise.Ecap.Entities.PrimaryEntities.StorageService.File;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using System.Collections.Generic;
using Aspose.Pdf;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFileVersionComparisonService : ILibraryFileVersionComparisonService
    {
        private readonly ILogger<LibraryFileVersionComparisonService> _logger;
        private readonly IStorageDataService _storageDataService;
        private readonly IPraxisFileService _fileService;
        private readonly ICompareLibraryFileVersionFactoryService _fileVersionFactoryService;
        private readonly IRepository _repository;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IDmsService _dmsService;
        private readonly ISecurityContextProvider _securityContextProvider;
        public LibraryFileVersionComparisonService(ILogger<LibraryFileVersionComparisonService> logger,
            IStorageDataService storageDataService,
            IPraxisFileService fileService,
            IRepository repository,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ICompareLibraryFileVersionFactoryService fileVersionFactoryService,
            IDmsService dmsService,
            ISecurityContextProvider securityContextProvider)
        {
            _repository = repository;
            _logger = logger;
            _storageDataService = storageDataService;
            _fileService = fileService;
            _fileVersionFactoryService = fileVersionFactoryService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _dmsService = dmsService;
            _securityContextProvider = securityContextProvider;
        }



        private ObjectArtifact GetObjectArtifactById(string objectArtifactId)
        {
            return _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId);
        }
        private ObjectArtifact GetObjectArtifactByFileStorageId(string fileStorageId)
        {
            return _repository.GetItem<ObjectArtifact>(o => o.FileStorageId == fileStorageId && o.Tags.Contains(TagName.IsAUpdateVersionFile));
        }

        private DocumentEditMappingRecord GetDocumentEditMappingRecords(string objectArtifactId)
        {
            return _repository.GetItem<DocumentEditMappingRecord>(o => o.ObjectArtifactId == objectArtifactId);
        }


        private bool IsValidFileType(string extension)
        {
            var validFileTypes = new[]
            {
                LibraryFileTypeEnum.DOCUMENT,
                //LibraryFileTypeEnum.PDF,
                LibraryFileTypeEnum.EXCELS
            };

            return validFileTypes.Contains(LibraryModuleFileFormats.GetFileFormat(extension));
        }
        public async Task<bool> HandleLibraryFileVersionComparison(string objectArtifactId)
        {
            _logger.LogInformation("Enter HandleLibraryFileVersionComparison for artifact ID: {Id}", objectArtifactId);

            try
            {
                var objectArtifact = GetObjectArtifactById(objectArtifactId);
                if (!ValidateObjectArtifact(objectArtifact, objectArtifactId)) return false;

                if (!IsValidFileType(objectArtifact.Extension))
                {
                    _logger.LogWarning("Invalid File type: {type}", objectArtifact.Extension);
                    return false;
                }

                var documentEditMappingRecord = GetDocumentEditMappingRecords(objectArtifactId);
                if (!ValidateDocumentEditMappingRecord(documentEditMappingRecord, objectArtifactId)) return false;

                var parentObjectArtifact = GetObjectArtifactById(documentEditMappingRecord.ParentObjectArtifactId);
                if (!ValidateParentObjectArtifact(parentObjectArtifact, documentEditMappingRecord.ParentObjectArtifactId)) return false;


                return await ProcessFileComparison(
                    objectArtifact,
                    objectArtifact.FileStorageId,
                    parentObjectArtifact.FileStorageId,
                    LibraryModuleFileFormats.GetFileFormat(objectArtifact.Extension),
                    GetFileName(objectArtifact.Name, $".{objectArtifact.Extension.ToLower()}")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Method} with error -> {Error} trace -> {Trace}", nameof(HandleLibraryFileVersionComparison), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private bool ValidateObjectArtifact(ObjectArtifact objectArtifact, string objectArtifactId)
        {
            if (objectArtifact == null)
            {
                _logger.LogError("Object artifact not found for artifact ID: {Id}", objectArtifactId);
                return false;
            }
            return true;
        }

        private bool ValidateDocumentEditMappingRecord(DocumentEditMappingRecord record, string objectArtifactId)
        {
            if (record == null || record.IsDraft)
            {
                _logger.LogInformation("DocumentEditMappingRecord not found for artifact ID: {Id}", objectArtifactId);
                return false;
            }
            return true;
        }

        private bool ValidateParentObjectArtifact(ObjectArtifact parentObjectArtifact, string parentObjectArtifactId)
        {
            if (parentObjectArtifact == null)
            {
                _logger.LogInformation("Parent Object artifact not found for ID: {Id}", parentObjectArtifactId);
                return false;
            }
            return true;
        }



        private ObjectArtifactFileUploadCommand PrepareObjectArifactDataForDmsFileUpload
        (
            ObjectArtifact objectArtifact,
            byte[] fileByteData,
            string fileName
        )
        {
            try
            {
                var metaData = new Dictionary<string, MetaValuePair>();
                if (objectArtifact.MetaData?.ContainsKey("DepartmentIdForSubscription") == true)
                {
                    metaData["DepartmentIdForSubscription"] = objectArtifact.MetaData["DepartmentIdForSubscription"];
                }
                return new ObjectArtifactFileUploadCommand()
                {
                    FileStorageId = Guid.NewGuid().ToString(),
                    Description = null,
                    Tags = objectArtifact.Tags,
                    ParentId = objectArtifact.ParentId,
                    FileName = fileName,
                    StorageAreaId = objectArtifact.StorageAreaId,
                    ObjectArtifactId = Guid.NewGuid().ToString(),
                    CorrelationId = Guid.NewGuid().ToString(),
                    WorkspaceId = objectArtifact.WorkSpaceId,
                    UserId = objectArtifact.OwnerId,
                    OrganizationId = objectArtifact.OrganizationId,
                    UseLicensing = true,
                    FileSizeInBytes = fileByteData.Length,
                    IsPreventShareWithParentSharedUsers = true,
                    FeatureId = "praxis-license",
                    MetaData = metaData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in PrepareObjectArifactDataForDmsFileUpload: {ex.Message} -> {ex.StackTrace}");
            }

            return null;
        }

        private static string GetFileName(string fileName, string fileType)
        {
            var lastDotIndex = fileName.LastIndexOf(fileType, StringComparison.Ordinal);

            if (lastDotIndex == -1) return fileName;

            var newName = fileName[..lastDotIndex];

            return newName + "_" + "version_compare" + fileType;
        }


        private async Task<bool> ProcessFileComparison(
       ObjectArtifact latestObjectArtifact,
       string newVersionFileId,
       string parentVersionFileId,
       LibraryFileTypeEnum fileType,
       string outputFileName)
        {
            try
            {
                var latestVersionFile = await _fileService.GetFileInfoFromStorage(newVersionFileId);
                var oldVersionFile = await _fileService.GetFileInfoFromStorage(parentVersionFileId);

                if (latestVersionFile == null || oldVersionFile == null)
                {
                    _logger.LogError("Failed to retrieve file metadata.");
                    return false;
                }

                var fileComparisonService = _fileVersionFactoryService.GetFileCompareService(fileType);
                if (fileComparisonService == null)
                {
                    _logger.LogError("No file comparison service available for file type: {fileType}", fileType);
                    return false;
                }

                await using var latestOriginalStream = await DownloadFileStreamFromUrl(latestVersionFile.Url);
                await using var oldOriginalStream = await DownloadFileStreamFromUrl(oldVersionFile.Url);

                // Buffer into memory (safe and reusable)
                using var latestBuffer = await ToMemoryStream(latestOriginalStream);
                using var oldBuffer = await ToMemoryStream(oldOriginalStream);

                // Clone the memory stream for each use
                using var latestForDelete = CloneMemoryStream(latestBuffer);
                using var oldForDelete = CloneMemoryStream(oldBuffer);

                using var latestForAdd = CloneMemoryStream(latestBuffer);
                using var oldForAdd = CloneMemoryStream(oldBuffer);

                var deletionOutputStream = await fileComparisonService.CompareDeleteFileVersionFromStream(
                    latestForDelete, oldForDelete);

                var addUpdateOutputStream = await fileComparisonService.CompareUpdateFileVersionFromStream(
                    latestForAdd, oldForAdd);

                if (deletionOutputStream == null || deletionOutputStream.Length == 0)
                {
                    _logger.LogWarning("Deletion comparison output is empty.");
                    return false;
                }

                await UploadFileToDMS(latestObjectArtifact, deletionOutputStream, outputFileName, ComparisonType.HighlightDeletions);

                if (addUpdateOutputStream != null && addUpdateOutputStream.Length > 0)
                {
                    await UploadFileToDMS(latestObjectArtifact, addUpdateOutputStream, outputFileName, ComparisonType.HighlightAdditions);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {MethodName}", nameof(ProcessFileComparison));
                return false;
            }
        }

        private async Task<Stream> DownloadFileStreamFromUrl(string fileUrl)
        {
            return await Task.Run(() => _storageDataService.GetFileContentStream(fileUrl));
        }
        private async Task<MemoryStream> ToMemoryStream(Stream input)
        {
            var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private MemoryStream CloneMemoryStream(MemoryStream original)
        {
            var clone = new MemoryStream(original.ToArray());
            clone.Position = 0;
            return clone;
        }

        private async Task<Boolean> UploadFileToDMS(
            ObjectArtifact objectArtifact,
            byte[] fileByteData,
            string fileName,
            ComparisonType comparisonType
            )
        {
            bool isUploaded = false;
            try
            {


                var dmsUploadPayload = PrepareObjectArifactDataForDmsFileUpload(objectArtifact, fileByteData, fileName);
                if (dmsUploadPayload != null)
                {
                    var securityContext = _securityContextProvider.GetSecurityContext();
                    var uploadUrl = await _dmsService.UploadFile(dmsUploadPayload, securityContext.OauthBearerToken);
                    _logger.LogInformation("uploadUrl: -> {UploadUrl}", uploadUrl);
                    if (!string.IsNullOrEmpty(uploadUrl))
                    {
                        isUploaded = await _storageDataService.UploadFileToStorageByUrlAsync(uploadUrl, fileByteData);
                        _logger.LogInformation("isUploaded: -> {IsUploaded}", isUploaded);
                        if (isUploaded)
                        {
                            await SaveComaparasionArtifactId(dmsUploadPayload, objectArtifact.ItemId, comparisonType);

                            _logger.LogInformation("Document Artifact uploaded sucessfully: {ArtifactId}", dmsUploadPayload.ObjectArtifactId);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                   "Exception occured in {Name} in UploadFileToStorage with error -> {Message} trace -> {StackTrace}", GetType().Name,
                   ex.Message, ex.StackTrace);
                isUploaded = false;
            }
            return isUploaded;
        }


        private async Task SaveComaparasionArtifactId(
          DmsFileUploadPayload dmsFileUploadPayload,
          string objectArtifactId,
          ComparisonType comparisonType
        )
        {
            var documentEditMappingRecord = GetDocumentEditMappingRecords(objectArtifactId);
            var parentVersion = GetParentObjectArtifactVersion(documentEditMappingRecord.ParentObjectArtifactId);

            if (comparisonType == ComparisonType.HighlightDeletions)
            {
                documentEditMappingRecord.VersionComparisonObjectArtifactId = dmsFileUploadPayload.ObjectArtifactId;
                documentEditMappingRecord.VersionComparisonFileStorageId = dmsFileUploadPayload.FileStorageId;
            }
            if (comparisonType == ComparisonType.HighlightAdditions)
            {
                documentEditMappingRecord.NewVersionComparisonObjectArtifactId = dmsFileUploadPayload.ObjectArtifactId;
                documentEditMappingRecord.NewVersionComparisonFileStorageId = dmsFileUploadPayload.FileStorageId;
            }

            documentEditMappingRecord.VersionComparisonObjectArtifactId = dmsFileUploadPayload.ObjectArtifactId;
            documentEditMappingRecord.VersionComparisonFileStorageId = dmsFileUploadPayload.FileStorageId;
            documentEditMappingRecord.ParentVersion = parentVersion;
            await _repository.UpdateAsync<DocumentEditMappingRecord>(cs => cs.ItemId.Equals(documentEditMappingRecord.ItemId), documentEditMappingRecord);
        }

        private string GetParentObjectArtifactVersion(string parentObjectArtifactId)
        {
            var objectArtifact = GetObjectArtifactById(parentObjectArtifactId);
            var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.VERSION.ToString()];
            var version = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifact?.MetaData, versionKey);
            return version ?? "1.0";
        }
        private async Task<string> UploadFileToStorage(
            string fileName,
            byte[] bytes
        )
        {
            try
            {
                var fileId = Guid.NewGuid().ToString();
                var success = await _storageDataService.UploadFileAsync(fileId, fileName, bytes);
                return success ? fileId : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {Name} in UploadFileToStorage with error -> {Message} trace -> {StackTrace}", GetType().Name,
                    ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }


        private bool IsAlreadyVersonCompareFileGenerated(DocumentEditMappingRecord record)
        {
            if (!string.IsNullOrEmpty(record.VersionComparisonFileStorageId)
                     && !string.IsNullOrEmpty(record.VersionComparisonObjectArtifactId))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> HandleLibraryFileVersionComparisonByFileStorageId(string fileStorageId)
        {

            try
            {
                var objectArtifact = GetObjectArtifactByFileStorageId(fileStorageId);
                if (objectArtifact == null) return false;
                _logger.LogInformation("Enter HandleLibraryFileVersionComparison for FileStorageId ID: {Id}", fileStorageId);

                if (!IsValidFileType(objectArtifact.Extension))
                {
                    _logger.LogError("Invalid File type: {type}", objectArtifact.Extension);
                    return false;
                }

                var documentEditMappingRecord = GetDocumentEditMappingRecords(objectArtifact.ItemId);
                if (documentEditMappingRecord == null) return false;

                if (IsAlreadyVersonCompareFileGenerated(documentEditMappingRecord)) return false;

                var parentObjectArtifact = GetObjectArtifactById(documentEditMappingRecord.ParentObjectArtifactId);
                if (!ValidateParentObjectArtifact(parentObjectArtifact, documentEditMappingRecord.ParentObjectArtifactId)) return false;


                return await ProcessFileComparison(
                    objectArtifact,
                    objectArtifact.FileStorageId,
                    parentObjectArtifact.FileStorageId,
                    LibraryModuleFileFormats.GetFileFormat(objectArtifact.Extension),
                    GetFileName(objectArtifact.Name, $".{objectArtifact.Extension.ToLower()}")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Method} with error -> {Error} trace -> {Trace}", nameof(HandleLibraryFileVersionComparison), ex.Message, ex.StackTrace);
                return false;
            }
        }
    }

    public enum ComparisonType
    {
        HighlightDeletions,
        HighlightAdditions
    }

}
