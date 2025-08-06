using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.GermanRailway;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class VectorDBFileService : IVectorDBFileService
    {
        private readonly ILogger<VectorDBFileService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly string _vectorDBServiceBaseUrl;
        private readonly string _fileUploadToVectorDBUrl;
        private readonly string _deleteFileFromVectorDBUrl;
        private readonly string _praxisWebUrl;
        private readonly string _praxisMonitorWebHookCommandUrl;

        public VectorDBFileService(
            ILogger<VectorDBFileService> logger,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient,
            IRepository repository,
            INotificationService notificationService
        )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _repository = repository;
            _notificationService = notificationService;
            _vectorDBServiceBaseUrl = configuration["VectorDBServiceBaseUrl"];
            _fileUploadToVectorDBUrl = configuration["FileUploadToVectorDBUrl"];
            _deleteFileFromVectorDBUrl = configuration["DeleteFileFromVectorDBUrl"];
            _praxisWebUrl = configuration["PraxisWebUrl"];
            _praxisMonitorWebHookCommandUrl = configuration["PraxisMonitorWebHookCommandUrl"];
        }

        public async Task<FileUploadToVectorDBResponse> UploadFile(List<FileUploadToVectorDBCommand> payloads)
        {
            try
            {
                var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;

                var httpResponse = await _serviceClient.SendToHttpAsync<FileUploadToVectorDBResponse>(
                    HttpMethod.Post,
                    _vectorDBServiceBaseUrl,
                    string.Empty,
                    _fileUploadToVectorDBUrl,
                    payloads,
                    token);

                if (httpResponse != null)
                {
                    _logger.LogInformation("FileUploadToVectorDB successful with response -> {HttpResponse}", JsonConvert.SerializeObject(httpResponse));

                    foreach ( var payload in payloads )
                    {
                        var status = httpResponse?.statuses?.FirstOrDefault(i => i.file_id == payload.file_id)?.status;

                        var updateCommand = new UpdateManualFileUploadStatusCommand
                        {
                            FileId = payload.file_id,
                            Status = status == "processed" ? "done": status,
                            SubscriptionFilter = payload.subscription_filter,
                        };

                        await UpdateManualFileUploadStatus(updateCommand);
                    }

                    return httpResponse;
                }

                _logger.LogError($"FileUploadToVectorDB failed with error -> {JsonConvert.SerializeObject(httpResponse)}");
                return httpResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }

            return null;
        }

        public async Task<bool> DeleteFile(DeleteFileFromVectorDBCommand payload)
        {
            try
            {
                var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
                var httpResponse = await _serviceClient.SendToHttpAsync<string>(
                    HttpMethod.Post,
                    _vectorDBServiceBaseUrl,
                    string.Empty,
                    _deleteFileFromVectorDBUrl,
                    payload,
                    token);

                if (httpResponse != null)
                {
                    _logger.LogInformation("DeleteFileFromVectorDB successful with response -> {HttpResponse}", JsonConvert.SerializeObject(httpResponse));
                    return true;
                }

                _logger.LogError($"DeleteFileFromVectorDB failed with error -> {JsonConvert.SerializeObject(httpResponse)}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }

            return false;
        }

        public async Task HandleManualFileUpload(ObjectArtifact objectArtifact)
        {
            if (objectArtifact.MetaData != null
                && objectArtifact.MetaData.TryGetValue("FileType", out var fileType) && fileType.Value == "9")
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                var payload = new FileUploadToVectorDBCommand
                {
                    file_id = objectArtifact.FileStorageId,
                    is_old_cluster = true,
                    tenant_id = securityContext.TenantId,
                    additional_key_value = new List<IAdditionalKeyValue>()
                    {
                        new IAdditionalKeyValue
                        {
                            key = "artifactId",
                            value = objectArtifact.ItemId
                        }
                    },
                    subscription_filter = new ISubscriptionFilter
                    {
                        Context = "FileUploadToVectorDBSuccessfullyNotification",
                        ActionName = "FileUploadToVectorDBSuccessfullyNotification",
                        Value = Guid.NewGuid().ToString()
                    },
                    webhook_url = $"{_praxisMonitorWebHookCommandUrl}/UpdateManualFileUploadStatus"
                };

                await UploadFile(new List<FileUploadToVectorDBCommand>() { payload });
            }
        }

        public async Task UpdateManualFileUploadStatus(UpdateManualFileUploadStatusCommand command)
        {
            try
            {
                var objectArtifact = await _repository.GetItemAsync<ObjectArtifact>(o => o.FileStorageId == command.FileId);

                if (objectArtifact.MetaData != null)
                {
                    var manualFileUploadStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.MANUAL_FILE_UPLOAD_STATUS}"];
                    var metaDataManualFileUploadStatusValue = new MetaValuePair()
                    {
                        Type = LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{ObjectArtifactMetaDataKeyTypeEnum.STRING}"],
                        Value = command?.Status?.ToString(),
                    };

                    if (objectArtifact.MetaData.TryGetValue(manualFileUploadStatusKey, out _))
                    {
                        objectArtifact.MetaData[manualFileUploadStatusKey] = metaDataManualFileUploadStatusValue;
                    }
                    else
                    {
                        objectArtifact.MetaData.Add(manualFileUploadStatusKey, metaDataManualFileUploadStatusValue);
                    }

                    await _repository.UpdateAsync<ObjectArtifact>(cs => cs.ItemId.Equals(objectArtifact.ItemId), objectArtifact);

                    var denormalizePayload = JsonConvert.SerializeObject(new
                    {
                        command.Status
                    });

                    var validStatuses = new HashSet<string> { "DONE", "FAILED" };

                    if (!string.IsNullOrWhiteSpace(command?.Status) && validStatuses.Contains(command.Status?.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        await _notificationService.GetCommonSubscriptionNotification(
                           true,
                           command.SubscriptionFilter.Value,
                           command.SubscriptionFilter.Context,
                           command.SubscriptionFilter.ActionName,
                           denormalizePayload
                       );
                    }

                    _logger.LogInformation("Successfully updated ObjectArtifact Manual File Upload Status for FileId: {FileId}", command.FileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in UpdateVectorDbFileUploadStatus with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

    }
}
