using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class ObjectArtifactDeleteService : IObjectArtifactDeleteService
    {
        private readonly IRepository _repository;
        private readonly ILogger<ObjectArtifactDeleteService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IStorageDataService _storageDataService;
        private readonly IServiceClient _serviceClient;
        private readonly string _licensingServiceUrl;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IVectorDBFileService _vectorDBFileService;
        private readonly ILibraryFileDeletedEventHandlerService _libraryFileDeletedEventHandlerService;

        public ObjectArtifactDeleteService
        (
            IRepository repository,
            ILogger<ObjectArtifactDeleteService> logger,
            ISecurityContextProvider securityContextProvider,
            IStorageDataService storageDataService,
            IServiceClient serviceClient,
            IConfiguration configuration,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ISecurityHelperService securityHelperService,
            IDepartmentSubscriptionService departmentSubscriptionService,
            IOrganizationSubscriptionService organizationSubscriptionService,
            IVectorDBFileService vectorDBFileService,
            ILibraryFileDeletedEventHandlerService libraryFileDeletedEventHandlerService
        )
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _storageDataService = storageDataService;
            _serviceClient = serviceClient;
            _licensingServiceUrl = configuration["LicensingBaseUrl"];
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _securityHelperService = securityHelperService;
            _departmentSubscriptionService = departmentSubscriptionService;
            _organizationSubscriptionService = organizationSubscriptionService;
            _vectorDBFileService = vectorDBFileService;
            _libraryFileDeletedEventHandlerService = libraryFileDeletedEventHandlerService;
        }

        public async Task<CommandResponse> DeleteObjectArtifact(string objectArtifactId)
        {
            var response = new CommandResponse();
            try
            {
                var artifact = GetObjectArtifactById(objectArtifactId);
                if (artifact != null)
                {
                    var allArtifactIds = GetAllLinkedObjectArtifactIds(artifact);
                    var batchSize = 100;
                    var batches = allArtifactIds.Chunk(batchSize);
                    foreach (var artifactIds in batches)
                    {
                        var objectArtifactList = _repository.GetItems<ObjectArtifact>(o => artifactIds.Contains(o.ItemId));
                        var fileArtifactIds = objectArtifactList.Where(o => o.ArtifactType == ArtifactTypeEnum.File).Select(o => o.ItemId).ToList();
                        var folderArtifactIds = objectArtifactList.Where(o => o.ArtifactType == ArtifactTypeEnum.Folder).Select(o => o.ItemId).ToList();

                        await _repository.DeleteAsync<ObjectArtifact>(o => artifactIds.Contains(o.ItemId));

                        foreach (var objArtifact in objectArtifactList)
                        {
                            await DeleteFileDependency(objArtifact);
                        }

                        if (fileArtifactIds.Count > 0)
                        {
                            await DeleteDraftedDocumentMappingData(fileArtifactIds);
                            await _libraryFileDeletedEventHandlerService.HandleLibraryFileDeletedEvent(fileArtifactIds);
                            await DeleteDependentModules(fileArtifactIds);
                        }
                    }

                    _logger.LogInformation("Total deleted Object artifacts -> {cnt}!", allArtifactIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in DeleteObjectArtifact: {ErrorMessage} -> {StackTrace}", ex.Message, ex.StackTrace);
                response.SetError("command", ex.Message);
            }
            return response;
        }

        private async Task DeleteDraftedDocumentMappingData(List<string> artifactIds)
        {
            try
            {
                await _repository.DeleteAsync<DocumentEditMappingRecord>(d => artifactIds.Contains(d.ItemId) && d.IsDraft);

                var updates = new Dictionary<string, object>()
                {
                    { nameof(ObjectArtifact.IsMarkedToDelete), true }
                };

                await _repository.UpdateManyAsync<ObjectArtifact>(m => artifactIds.Contains(m.ItemId), updates);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in DeleteDraftedDocumentMappingData: {ex.Message} -> {ex.StackTrace}");
            }
        }

        private async Task DeleteDependentModules(List<string> artifactIds)
        {
            await _repository.DeleteAsync<DmsArtifactUsageReference>(o => artifactIds.Contains(o.ObjectArtifactId));
        }

        private async Task DeleteFileDependency(ObjectArtifact objectArtifact)
        {
            if (objectArtifact.ArtifactType == ArtifactTypeEnum.File)
            {
                try
                {
                    var isDeletedFile = this._storageDataService.DeleteFile(
                        new List<string> { objectArtifact.FileStorageId },
                        _securityContextProvider.GetSecurityContext().OauthBearerToken
                    ).Result;

                    if (isDeletedFile)
                    {
                        _logger.LogInformation("+Object artifact deleted successfully from cloud storage!");
                        await DeleteStorageFromSubscription(objectArtifact);
                        await DeleteFileFromVectorDB(objectArtifact);
                        UpdateLicensingUsage(objectArtifact, objectArtifact.OrganizationId, "praxis-license", _securityContextProvider.GetSecurityContext().OauthBearerToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occured in DeleteFile -> {ex.Message}");
                }
            }
        }

        private void UpdateLicensingUsage(ObjectArtifact objectArtifact, string orgId, string featureId, string token)
        {
            if (orgId != null && featureId != null)
            {
                var updateUsageCommand = new
                {
                    FeatureId = featureId,
                    OrganizationId = orgId,
                    QuotaUsed = objectArtifact.FileSizeInByte < 0
                    ? objectArtifact.FileSizeInByte
                    : objectArtifact.FileSizeInByte * -1
                };

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_licensingServiceUrl + "Licensing/Executor/UpdateUsage"),
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(updateUsageCommand),
                        Encoding.UTF8,
                        "application/json")
                };
                request.Headers.Add("Authorization", $"bearer {token}");

                HttpResponseMessage response = _serviceClient.SendToHttpAsync(request).Result;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to UpdateLicensingUsage");
                }

                _logger.LogInformation("Licensing update usage call finished: {Response}", JsonConvert.SerializeObject(response));
            }
        }

        private ObjectArtifact GetObjectArtifactById(string objectArtifactId)
        {
            return _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId);
        }

        private List<string> GetAllLinkedObjectArtifactIds(ObjectArtifact objectArtifact)
        {
            var allArtifactIds = new List<string>();
            if (objectArtifact == null) return allArtifactIds;

            allArtifactIds.Add(objectArtifact.ItemId);
            var artifactIds = new List<ObjectArtifact> { objectArtifact }.Where(o => o.ArtifactType == ArtifactTypeEnum.Folder).Select(o => o.ItemId).ToList();
            while (artifactIds.Count > 0)
            {
                var children = _repository.GetItems<ObjectArtifact>(item => artifactIds.Contains(item.ParentId)).Select(s => new { s.ItemId, s.ArtifactType }).ToList();
                allArtifactIds.AddRange(children.Select(s => s.ItemId).ToList());
                artifactIds = children.Where(s => s.ArtifactType == ArtifactTypeEnum.Folder).Select(s => s.ItemId).ToList();
            }
            return allArtifactIds;
        }

        private async Task DeleteStorageFromSubscription(ObjectArtifact objectArtifact) 
        {
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentIdForSubscription(objectArtifact.MetaData);
            await _departmentSubscriptionService.DeleteStorageFromDepartmentSubscriptionAsync(departmentId, objectArtifact.FileSizeInByte);
            await _organizationSubscriptionService.DeleteStorageFromOrganizationSubscriptionAsync(objectArtifact.OrganizationId, objectArtifact.FileSizeInByte);
        }

        private async Task DeleteFileFromVectorDB(ObjectArtifact objectArtifact)
        {
            if (objectArtifact.MetaData != null && objectArtifact.MetaData.TryGetValue("FileType", out var fileType) && fileType.Value == "9")
            {
                var payload = new DeleteFileFromVectorDBCommand
                {
                    file_id = objectArtifact.FileStorageId,
                    filter_key_value_pair = new List<IFilterKeyValue>()
                };

                await _vectorDBFileService.DeleteFile(payload);
            }
        }
    }
}
