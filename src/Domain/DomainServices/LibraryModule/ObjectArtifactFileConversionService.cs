using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Aspose.Pdf;
using System.IO.Compression;
using System.Diagnostics;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class ObjectArtifactFileConversionService : IObjectArtifactFileConversionService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IDocGenerationService _docGenerationService;
        private readonly IPraxisFileService _fileService;
        private readonly IStorageDataService _storageDataService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ObjectArtifactFileConversionService> _logger;
        private readonly IDocumentEditMappingService _documentEditMappingService;
        private readonly IDmsService _dmsService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IServiceClient _serviceClient;
        private readonly ILibraryFileVersionComparisonService _libraryFileVersionComparisonService;
        private readonly ILibraryStandardDocumentService _libraryStandardDocumentService;

        public ObjectArtifactFileConversionService
        (
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IDocGenerationService docGenerationService,
            IPraxisFileService fileService,
            IStorageDataService storageDataService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            INotificationService notificationService,
            ILogger<ObjectArtifactFileConversionService> logger,
            IDocumentEditMappingService documentEditMappingService,
            IDmsService dmsService,
            ISecurityHelperService securityHelperService,
            IServiceClient serviceClient,
            ILibraryFileVersionComparisonService libraryFileVersionComparisonService,
            ILibraryStandardDocumentService libraryStandardDocumentService)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _docGenerationService = docGenerationService;
            _fileService = fileService;
            _storageDataService = storageDataService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _notificationService = notificationService;
            _logger = logger;
            _documentEditMappingService = documentEditMappingService;
            _dmsService = dmsService;
            _securityHelperService = securityHelperService;
            _serviceClient = serviceClient;
            _libraryFileVersionComparisonService = libraryFileVersionComparisonService;
            _libraryStandardDocumentService = libraryStandardDocumentService;
        }

        public async Task MakeACopyHtmlFileId(GetHtmlFileIdFromObjectArtifactDocumentCommand command)
        {
            var newHtmlFileId = string.Empty;
            try
            {
                var originalHtmlFileId = string.Empty;
                var securityContext = _securityContextProvider.GetSecurityContext();
                var objectArtifactData = GetObjectArtifactById(command.ObjectArtifactId);
                var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE}"];
                if (objectArtifactData != null && _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData.MetaData,
                        isChildStandardFileKey) == "1" && !_objectArtifactUtilityService.IsADocument(objectArtifactData?.MetaData, true))
                {
                    var documentMappingData =
                        _repository.GetItem<DocumentEditMappingRecord>(d =>
                            d.ObjectArtifactId == command.ObjectArtifactId);
                    newHtmlFileId = documentMappingData.CurrentHtmlFileId;
                }
                else if (objectArtifactData != null)
                {
                    if (objectArtifactData.MetaData != null && _objectArtifactUtilityService.IsADocument(objectArtifactData?.MetaData, true))
                    {
                        var documentMappingData = await _documentEditMappingService.GetDocumentMappingDraftHtmlFileInfo(objectArtifactData.ItemId);

                        if (documentMappingData != null)
                        {
                            newHtmlFileId = documentMappingData.CurrentHtmlFileId;
                        }
                    } 
                    else
                    {
                        var documentMappingData = 
                            await _documentEditMappingService.GetDocumentMappingDraftHtmlFileInfoByParentArtifactId(objectArtifactData.ItemId);

                        if (documentMappingData == null)
                        {
                            if (!string.IsNullOrEmpty(objectArtifactData.FileStorageId))
                            {
                                originalHtmlFileId = objectArtifactData.FileStorageId; // await GetOrGenerateOriginalHtmlFileIdAsync(objectArtifactData);
                            }

                            if (!string.IsNullOrEmpty(originalHtmlFileId))
                            {
                                newHtmlFileId = await CloneFromFileId(originalHtmlFileId);
                                
                                var file = await _fileService.GetFileInfoFromStorage(newHtmlFileId);
                                if (file == null) newHtmlFileId = string.Empty;
                                else
                                {
                                    var payload = new CreateDocumentEditUrlPayload()
                                    {
                                        fileName = objectArtifactData.Name,
                                        fileUrl = file?.Url
                                    };
                                    newHtmlFileId = (await _fileService.CreateDocumentEditUrl(payload))?.editUrl;
                                }

                                if (!string.IsNullOrEmpty(newHtmlFileId))
                                {
                                    await _documentEditMappingService.CreateDocumentEditMappingRecord(objectArtifactData, newHtmlFileId, originalHtmlFileId, securityContext);
                                }
                            }
                        }
                        else
                        {
                            newHtmlFileId = documentMappingData.CurrentHtmlFileId;
                        }
                    }
                }
                var denormalizePayload = JsonConvert.SerializeObject(new
                {
                    HtmlFileId = newHtmlFileId
                });
                await _notificationService.GetHtmlFileIdFromObjectArtifactDocumentSubscriptionNotification
                    (!string.IsNullOrEmpty(newHtmlFileId), command.SubscriptionId, denormalizePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in MakeACopyHtmlFileId: {ErrorMessage} -> {StackTrace}", ex.Message, ex.StackTrace);
                await _notificationService.GetHtmlFileIdFromObjectArtifactDocumentSubscriptionNotification
                    (false, command.SubscriptionId);
            }
        }
        private async Task<string> GetOrGenerateOriginalHtmlFileIdAsync(ObjectArtifact objectArtifactData)
        {
            var htmlFileIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.PROCESSED_ORIGINAL_HTML_FILE_ID}"];

            var originalHtmlFileId = _objectArtifactUtilityService.GetMetaDataValueByKey(
                objectArtifactData.MetaData, htmlFileIdKey);

            if (string.IsNullOrEmpty(originalHtmlFileId))
            {
                originalHtmlFileId = await ConvertToHtmlAndUpload(objectArtifactData.FileStorageId);
            }

            return originalHtmlFileId;
        }


        public async Task ProcessObjectArtifactHtmlDocument(ProcessDraftedObjectArtifactDocumentCommand command)
        {
            _logger.LogInformation("Processing Object Artifact Html Document for ObjectArtifactId: {ObjectArtifactId}", command.ObjectArtifactId);
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var documentMappingData = await _documentEditMappingService.GetDocumentEditMappingRecordByDraftArtifact(command.ObjectArtifactId);

                _logger.LogInformation("Document Mapping Data: {DocumentMappingData}", JsonConvert.SerializeObject(documentMappingData));

                var objectArtifact = GetObjectArtifactById(command.ObjectArtifactId);
                var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE}"];
                var isChildStandardFile = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifact?.MetaData, isChildStandardFileKey) == "1";
                if (objectArtifact != null && isChildStandardFile && !_objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true))
                {
                    _logger.LogInformation("Processing Child Standard File");
                    var docByteData = await ConvertToDocumentInBytes(command.HtmlFileId);
                    await _storageDataService.UploadFileAsync(objectArtifact.FileStorageId, objectArtifact.Name, docByteData, objectArtifact.Tags);
                    documentMappingData = _repository.GetItem<DocumentEditMappingRecord>(d =>
                            d.ObjectArtifactId == command.ObjectArtifactId);
                    if (documentMappingData != null)
                    {
                        documentMappingData.CurrentDocFileId = objectArtifact.FileStorageId;
                        await _libraryStandardDocumentService.UpdateDocumentEditRecordHistory(documentMappingData);
                        await _documentEditMappingService.UpdateDocumentEditMetaData(documentMappingData);
                    }
                }
                else if (documentMappingData != null && objectArtifact != null)
                {
                    _logger.LogInformation("Processing Document Mapping Data for General Document");
                    var docByteData = await ConvertToDocumentInBytes(command.HtmlFileId);
                    if (docByteData != null && docByteData.Length > 0)
                    {
                        var dmsUploadPayload = PrepareObjectArifactDataForDmsFileUpload(command, objectArtifact, docByteData);
                        _logger.LogInformation("DMS Upload Payload: {DmsUploadPayload}", JsonConvert.SerializeObject(dmsUploadPayload));
                        if (dmsUploadPayload != null)
                        {
                            dmsUploadPayload.Tags= dmsUploadPayload.Tags.Concat(new[] { TagName.IsAUpdateVersionFile }).Distinct().ToArray();
                            var uploadUrl = await _dmsService.UploadFile(dmsUploadPayload, securityContext.OauthBearerToken);
                            _logger.LogInformation("Upload Url: {UploadUrl}", uploadUrl);
                            if (!string.IsNullOrEmpty(uploadUrl))
                            {
                                var isUploaded = await _storageDataService.UploadFileToStorageByUrlAsync(uploadUrl, docByteData);
                                _logger.LogInformation("Is Uploaded: {IsUploaded}", isUploaded);
                                if (isUploaded)
                                {
                                    await _documentEditMappingService.SaveObjectArtifactDocumentDraftedData(dmsUploadPayload, objectArtifact.ItemId);
                                    _logger.LogInformation("Have To Notify Cockpit? The Answer is: {Answer}", command.IsNotifyToCockpit);

                                    if (command.IsNotifyToCockpit)
                                    {
                                        PublishLibraryFileEditedByOthersEvent(dmsUploadPayload.ObjectArtifactId);
                                    }
                                    
                                    _logger.LogInformation("Document Artifact uploaded successfully: ArtifactId: -> {ObjectArtifactId}", dmsUploadPayload.ObjectArtifactId);
                                    if (objectArtifact.MetaData != null && _objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true))
                                    {
                                        await _dmsService.DeleteObjectArtifact(objectArtifact.ItemId, objectArtifact.OrganizationId);
                                    }
                                    await _notificationService.GetCommonSubscriptionNotification
                                    (
                                        true,
                                        command.SubscriptionId,
                                        command.SubscriptionContext,
                                        command.SubscriptionActionName
                                    );
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured in ProcessObjectArtifactHtmlDocument: {ex.Message} -> {ex.StackTrace}");
            }
            
            await _notificationService.GetCommonSubscriptionNotification
            (
                false,
                command.SubscriptionId,
                command.SubscriptionContext,
                command.SubscriptionActionName
            );
        }
        private void LogFileSize(string label, long byteLength)
        {
            double sizeMb = Math.Round((double)byteLength / (1024 * 1024), 2);
            _logger.LogInformation("{Label} size: {SizeMB} MB", label, sizeMb);
        }

        public async Task<string> ConvertToHtmlAndUpload(string fileId)
        {
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.Now;
            _logger.LogInformation("Starting document conversion at {StartTime}", startTime);

            try
            {
                var file = await _fileService.GetFileInfoFromStorage(fileId);
                if (file == null)
                {
                    _logger.LogWarning("File not found for fileId: {FileId}", fileId);
                    return string.Empty;
                }
                

                await using Stream docStream = _storageDataService.GetFileContentStream(file.Url);
                _logger.LogInformation("Document stream retrieved from storage");
                LogFileSize("Original DOCX file size", docStream.Length);

                var htmlByteData = _docGenerationService.PrepareHtmlFromObjectArtifactDocumentStream(docStream);
               
                if (htmlByteData == null || htmlByteData.Length == 0)
                {
                    _logger.LogWarning("HTML conversion returned empty result");
                    return string.Empty;
                }
                _logger.LogInformation("DOCX to HTML conversion completed");
                LogFileSize("Generated HTML  file size", htmlByteData.Length);

                // Compress HTML
                byte[] compressedHtmlBytes;
                await using (var input = new MemoryStream(htmlByteData))
                await using (var output = new MemoryStream())
                {
                    using var gzipStream = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
                    await input.CopyToAsync(gzipStream);
                    gzipStream.Close(); // Finalize compression
                    compressedHtmlBytes = output.ToArray();
                }

                LogFileSize("Compressed HTML file size", compressedHtmlBytes.Length);
                // Prepare output filename
                string fileName = file.Name ?? "";
                string htmlFileId = Guid.NewGuid().ToString();

                var success = await _storageDataService.UploadFileAsyncAsGzip(
                    htmlFileId,
                    $"{fileName}.html",
                    compressedHtmlBytes,
                    contentType: "text/html",
                    contentEncoding: "gzip"
                );

                stopwatch.Stop();
                var endTime = DateTime.Now;
                _logger.LogInformation(
                    "Conversion completed at {EndTime}, duration: {Duration}",
                    endTime, stopwatch.Elapsed
                );

                if (success)
                {
                    return htmlFileId;
                }
                else
                {
                    _logger.LogWarning("File upload failed for fileId: {FileId}", fileId);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Exception in ConvertToHtmlAndUpload for fileId: {FileId}", fileId);
                return string.Empty;
            }
        }

        private async Task<Stream> GetOriginalDocStream(string objectArtifactId)
        {
            var objectArtifact = await _repository.GetItemAsync<ObjectArtifact>(o =>
                           o.ItemId == objectArtifactId);
            if (objectArtifact == null) return null;
            

            var file = await _fileService.GetFileInfoFromStorage(objectArtifact.FileStorageId);
            if (file == null) return null;
            Stream htmlStream = _storageDataService.GetFileContentStream(file.Url);
            return htmlStream;
        }

        private async Task<byte[]> ConvertToDocumentInBytes(string fileId)
        {
            try
            {
                var file = await _fileService.GetFileInfoFromStorage(fileId);
                if (file != null)
                {
                   
                    Stream htmlStream = _storageDataService.GetFileContentStream(file.Url);
                    var docByteData = _docGenerationService.PrepareObjectArtifactDocumentFromHtmlStream(htmlStream);
                    htmlStream.Close();
                    htmlStream.Dispose();
                    return docByteData;
                }

                return new byte[] { };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ConvertToDocumentAndUpload: {ex.Message} -> {ex.StackTrace}");
                return new byte[] { };
            }
        }

        private async Task<string> CloneFromFileId(string fileId)
        {
            try
            {
                var clonedFile = await _fileService.CloneFiles(new List<string>() { fileId });
                if (clonedFile != null && clonedFile.Count() > 0) return clonedFile.First().NewFileId;

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in CloneFromFileId: {ErrorMessage} -> {StackTrace}", ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }

        private ObjectArtifactFileUploadCommand PrepareObjectArifactDataForDmsFileUpload
        (
            ProcessDraftedObjectArtifactDocumentCommand command,
            ObjectArtifact objectArtifact,
            byte[] docByteData
        )
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var metaData = _dmsService.PrepareMetaDataForDmsDocumentFileUpload(objectArtifact, _objectArtifactUtilityService.IsADocument(objectArtifact?.MetaData, true), false, command.IsNotifyToCockpit); 
                return new ObjectArtifactFileUploadCommand()
                {
                    FileStorageId = Guid.NewGuid().ToString(),
                    Description = null,
                    Tags = objectArtifact.Tags,
                    ParentId = objectArtifact.ParentId,
                    FileName = objectArtifact.Name,
                    StorageAreaId = objectArtifact.StorageAreaId,
                    ObjectArtifactId = Guid.NewGuid().ToString(),
                    CorrelationId = command.SubscriptionId,
                    WorkspaceId = command.WorkspaceId,
                    UserId = securityContext.UserId,
                    OrganizationId = objectArtifact.OrganizationId,
                    UseLicensing = command.UseLicensing,
                    FileSizeInBytes = docByteData.Length,
                    IsPreventShareWithParentSharedUsers = true,
                    FeatureId = "praxis-license",
                    MetaData = metaData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in PrepareObjectArifactDataForDmsFileUpload: {ErrorMessage} -> {StackTrace}", ex.Message, ex.StackTrace);
            }

            return null;
        }

        private ObjectArtifact GetObjectArtifactById(string objectArtifactId)
        {
            return _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId);
        }

        private void PublishLibraryFileEditedByOthersEvent(string objectArtifactId)
        {
            _logger.LogInformation("Publishing {EventName} event for objectArtifactId: {objectArtifactId}", nameof(PraxisEventType.LibraryFileEditedByOthersEvent), objectArtifactId);
            
            var editedByOthersEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileEditedByOthersEvent,
                JsonPayload = JsonConvert.SerializeObject(objectArtifactId)
            };
            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), editedByOthersEvent);

            _logger.LogInformation("Published {EventName} event with payload: {EventPayload}", nameof(PraxisEventType.LibraryFileEditedByOthersEvent), JsonConvert.SerializeObject(editedByOthersEvent));

        }
    }
}
