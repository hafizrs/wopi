using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using System.Collections.Concurrent;
using System.Threading;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactService : IObjectArtifactService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IChangeLogService _changeLogService;
        private readonly IRepository _repository;
        private readonly IDocumentKeywordService _documentKeywordService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IStorageDataService _storageDataService;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<ObjectArtifactService> _logger;
        private readonly ILincensingService _licensingService;
        private readonly IObjectArtifactValidationService _objectArtifactValidationService;
        private readonly IDmsFolderCreatedEventHandlerHandlerService _dmsFolderCreatedEventHandlerHandlerService;
        private readonly IMongoClientRepository _mongoClientRepository;

        public ObjectArtifactService(
            ISecurityContextProvider securityContextProvider,
            IChangeLogService changeLogService,
            IRepository repository,
            IDocumentKeywordService documentKeywordService,
            IObjectArtifactSearchService objectArtifactSearchService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IStorageDataService storageDataService,
            IServiceClient serviceClient,
            ILogger<ObjectArtifactService> logger,
            ILincensingService licensingService,
            IObjectArtifactValidationService objectArtifactValidationService,
            IDmsFolderCreatedEventHandlerHandlerService dmsFolderCreatedEventHandlerHandlerService,
            IMongoClientRepository mongoClientRepository
        )
        {
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
            _repository = repository;
            _documentKeywordService = documentKeywordService;
            _objectArtifactSearchService = objectArtifactSearchService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _storageDataService = storageDataService;
            _serviceClient = serviceClient;
            _logger = logger;
            _licensingService = licensingService;
            _objectArtifactValidationService = objectArtifactValidationService;
            _dmsFolderCreatedEventHandlerHandlerService = dmsFolderCreatedEventHandlerHandlerService;
            _mongoClientRepository = mongoClientRepository;
        }

        public async Task<CommandResponse> InitiateObjectArtifactFolderCreateAsync(ObjectArtifactFolderCreateCommand createFolderCommand)
        {
            var commandResponse = new CommandResponse();

            StorageArea storageArea = GetStorageArea(createFolderCommand.StorageAreaId);
            var workSpace = await GetWorkSpace(createFolderCommand.WorkspaceId);

            var isValid = _objectArtifactValidationService.ValidateCreateFolder(createFolderCommand, commandResponse, workSpace, storageArea);

            if (isValid)
            {
                createFolderCommand.StorageAreaId = storageArea.ItemId;
                createFolderCommand.WorkspaceId = workSpace.ItemId;

                await CreateFolder(createFolderCommand, workSpace, commandResponse);
            }

            if (commandResponse.Errors.IsValid)
            {
                var libraryFolderCreatedEvent = new GenericEvent()
                {
                    EventType = PraxisEventType.LibraryFolderCreatedEvent,
                    JsonPayload = JsonConvert.SerializeObject(createFolderCommand)
                };

                _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryFolderCreatedEvent);
            }

            return commandResponse;
        }

        public async Task<List<ObjectArtifactFolderCreateCommand>> InitiateObjectArtifactFolderListCreateAsync(List<ObjectArtifactFolderCreateCommand> createFolderListCommand)
        {
            var responses = new List<ObjectArtifactFolderCreateCommand>();

            var objectArtifactCommandList = new List<ObjectArtifact>();

            StorageArea storageArea = GetStorageArea(createFolderListCommand.FirstOrDefault().StorageAreaId);
            var workSpace = await GetWorkSpace(createFolderListCommand.FirstOrDefault().WorkspaceId);

            foreach (var createFolderCommand in createFolderListCommand)
            {
                var commandResponse = new CommandResponse();
                var isValid = _objectArtifactValidationService.ValidateCreateFolder(createFolderCommand, commandResponse, workSpace, storageArea, true);
                if (isValid)
                {
                    createFolderCommand.StorageAreaId = storageArea.ItemId;
                    createFolderCommand.WorkspaceId = workSpace.ItemId;
                    var command = await CreateFolderCommand(createFolderCommand, workSpace);
                    objectArtifactCommandList.Add(command);
                    responses.Add(createFolderCommand);
                }
            }

            if (objectArtifactCommandList.Any())
            {
                var collection = _mongoClientRepository.GetCollection<ObjectArtifact>();

                const int batchSize = 100;

                foreach (var batch in objectArtifactCommandList.Chunk(batchSize))
                {
                    await collection.InsertManyAsync(batch);
                    _logger.LogInformation("Inserted batch of {BatchSize} ObjectArtifacts", batch.Count());
                }
            }

            if (responses.Count > 0)
            {
                await _dmsFolderCreatedEventHandlerHandlerService.HandleDmsFolderListCreatedEvent(responses);
            }

            return responses;
        }

        private async Task<ObjectArtifact> CreateFolderCommand(ObjectArtifactFolderCreateCommand createFolderCommand, Workspace workSpace)
        {
            var user = await _repository.GetItemAsync<User>(u => u.ItemId == createFolderCommand.UserId);
            string ownerName = user?.DisplayName ?? string.Empty;
            var roles = new string[] { RoleNames.Admin };
            var ids = new string[] { createFolderCommand.UserId };

            var objectArtifact = new ObjectArtifact
            {
                ItemId = createFolderCommand.ObjectArtifactId ?? Guid.NewGuid().ToString(),
                Name = createFolderCommand.Name,
                OwnerId = createFolderCommand.UserId,
                OwnerName = ownerName,
                Description = createFolderCommand.Description,
                ArtifactType = ArtifactTypeEnum.Folder,
                ParentId = createFolderCommand.ParentId,
                StorageAreaId = createFolderCommand.StorageAreaId,
                WorkSpaceId = createFolderCommand.WorkspaceId,
                WorkSpaceName = workSpace.Name,
                Tags = createFolderCommand.Tags,
                CreatedBy = createFolderCommand.UserId,
                CreateDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                Language = "en-US",
                LastUpdatedBy = createFolderCommand.UserId,
                MetaData = createFolderCommand.MetaData,
                OrganizationId = createFolderCommand?.OrganizationId,
                Color = createFolderCommand.Color,
                RolesAllowedToRead = roles,
                IdsAllowedToRead = ids,
                RolesAllowedToUpdate = roles,
                IdsAllowedToUpdate = ids,
                RolesAllowedToWrite = roles,
                IdsAllowedToWrite = ids,
                RolesAllowedToDelete = roles,
                IdsAllowedToDelete = ids
            };

            if (objectArtifact.ItemId == objectArtifact.OrganizationId && createFolderCommand.IsAOrganizationFolder)
            {
                objectArtifact.SharedOrganizationList = new List<SharedOrganizationInfo>()
                    {
                        new SharedOrganizationInfo
                        {
                            OrganizationId = objectArtifact.OrganizationId,
                            SharedPersonList = new List<string>(),
                            Tags = new string[] { RoleNames.Organization_Read_Dynamic }
                        }
                    };
                objectArtifact.SharedPersonIdList = new List<string>();
                objectArtifact.SharedRoleList = new List<string>
                    {
                        $"{RoleNames.Organization_Read_Dynamic}_{objectArtifact.OrganizationId}"
                    };
                var tags = new List<string> { $"{RoleNames.Organization_Read_Dynamic}_{objectArtifact.OrganizationId}" };
                objectArtifact.RolesAllowedToRead = objectArtifact.RolesAllowedToRead.Concat(tags).Distinct().ToArray();
                objectArtifact.Tags = tags.ToArray();
            }

            return objectArtifact;
        }

        public async Task<CommandResponse> InitiateObjectArtifactFileUploadAsync(ObjectArtifactFileUploadCommand uploadFileCommand)
        {
            var commandResponse = new CommandResponse();

            if (uploadFileCommand.UseLicensing)
            {

                bool hasLicense = await HasLicenseOrLimit(uploadFileCommand);

                if (!hasLicense)
                {
                    commandResponse.SetError("command", "Invalid License or Quota Exceeded or FileSize");
                    return commandResponse;
                }
            }

            StorageArea storageArea = GetStorageArea(uploadFileCommand.StorageAreaId);
            Workspace workSpace = await GetWorkSpace(uploadFileCommand.WorkspaceId);

            bool isValid = _objectArtifactValidationService.ValidateUploadFile(uploadFileCommand, commandResponse, workSpace, storageArea);

            if (!isValid)
            {
                return commandResponse;
            }

            uploadFileCommand.StorageAreaId = storageArea.ItemId;
            uploadFileCommand.WorkspaceId = workSpace.ItemId;

            var isFileExist = FileAlreadyExistInStorage(uploadFileCommand.FileStorageId);
            if (!isFileExist) await UploadFileToStorage(commandResponse, uploadFileCommand);
            else _logger.LogInformation("FileId entry already exists, Creating only Object Artifact");

            ObjectArtifact artifact = null;

            if (commandResponse.Errors.IsValid)
            {
                artifact = await CreateFileArtifact(uploadFileCommand, workSpace);
            }

            if (commandResponse.Errors.IsValid && artifact != null)
            {
                if (uploadFileCommand.IsUploadFromInterface == false)
                {
                    var libraryFileUploadedEvent = new GenericEvent
                    {
                        EventType = PraxisEventType.LibraryFileUploadedEvent,
                        JsonPayload = JsonConvert.SerializeObject(uploadFileCommand)
                    };

                    _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryFileUploadedEvent);
                }

            }

            return commandResponse;
        }

        private bool FileAlreadyExistInStorage(string fileId)
        {
            var fileInStorage = _repository.ExistsAsync<File>(item => item.ItemId == fileId).GetAwaiter().GetResult();

            return fileInStorage;
        }

        private async Task CreateFolder(ObjectArtifactFolderCreateCommand createFolderCommand, Workspace workSpace, CommandResponse commandResponse)
        {
            try
            {
                string ownerName = _repository.GetItem<User>(u => u.ItemId == createFolderCommand.UserId)?.DisplayName;
                var roles = new string[] { RoleNames.Admin };
                var ids = new string[] { createFolderCommand.UserId };

                var objectArtifact = new ObjectArtifact
                {
                    ItemId = createFolderCommand.ObjectArtifactId ?? Guid.NewGuid().ToString(),
                    Name = createFolderCommand.Name,
                    OwnerId = createFolderCommand.UserId,
                    OwnerName = ownerName,
                    Description = createFolderCommand.Description,
                    ArtifactType = ArtifactTypeEnum.Folder,
                    ParentId = createFolderCommand.ParentId,
                    StorageAreaId = createFolderCommand.StorageAreaId,
                    WorkSpaceId = createFolderCommand.WorkspaceId,
                    WorkSpaceName = workSpace.Name,
                    Tags = createFolderCommand.Tags,
                    CreatedBy = createFolderCommand.UserId,
                    CreateDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    Language = "en-US",
                    LastUpdatedBy = createFolderCommand.UserId,
                    MetaData = createFolderCommand.MetaData,
                    OrganizationId = createFolderCommand?.OrganizationId,
                    Color = createFolderCommand.Color,
                    RolesAllowedToRead = roles,
                    IdsAllowedToRead = ids,
                    RolesAllowedToUpdate = roles,
                    IdsAllowedToUpdate = ids,
                    RolesAllowedToWrite = roles,
                    IdsAllowedToWrite = ids,
                    RolesAllowedToDelete = roles,
                    IdsAllowedToDelete = ids
                };

                if (objectArtifact.ItemId == objectArtifact.OrganizationId && createFolderCommand.IsAOrganizationFolder)
                {
                    objectArtifact.SharedOrganizationList = new List<SharedOrganizationInfo>()
                    {
                        new SharedOrganizationInfo
                        {
                            OrganizationId = objectArtifact.OrganizationId,
                            SharedPersonList = new List<string>(),
                            Tags = new string[] { RoleNames.Organization_Read_Dynamic }
                        }
                    };
                    objectArtifact.SharedPersonIdList = new List<string>();
                    objectArtifact.SharedRoleList = new List<string>
                    {
                        $"{RoleNames.Organization_Read_Dynamic}_{objectArtifact.OrganizationId}"
                    };
                    var tags = new List<string> { $"{RoleNames.Organization_Read_Dynamic}_{objectArtifact.OrganizationId}" };
                    objectArtifact.RolesAllowedToRead = objectArtifact.RolesAllowedToRead.Concat(tags).Distinct().ToArray();
                    objectArtifact.Tags = tags.ToArray();
                }

                await _repository.SaveAsync(objectArtifact);

                commandResponse.RequestUri = objectArtifact.ItemId;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in CreateFolder -> {message}", ex.Message);
                commandResponse.SetError("command", ex.Message);
            }
        }

        private async Task UploadFileToStorage(CommandResponse commandResponse, ObjectArtifactFileUploadCommand uploadFileCommand)
        {
            _logger.LogInformation("Going to generated presigned url for file upload...");

            var query = new PreSignedUrlForUploadQueryModel
            {
                //ItemId = objectArtifact.FileStorageId,
                ItemId = uploadFileCommand.FileStorageId,
                Name = uploadFileCommand.FileName,
                //Name = objectArtifact.Name,
                ParentDirectoryId = "Dms-parent-diretory",
                Tags = uploadFileCommand.Tags != null && uploadFileCommand.Tags.Any()
                    ? JsonConvert.SerializeObject(uploadFileCommand.Tags)
                    : "[\"test\"]",
                MetaData = uploadFileCommand.MetaData != null
                    ? JsonConvert.SerializeObject(uploadFileCommand.MetaData)
                    : string.Empty
            };

            GetPreSignedUrlForUploadResponse response;

            try
            {
                response = await _storageDataService.GetPreSignedUrlForUploadQueryModel(query);
                if (string.IsNullOrWhiteSpace(response?.UploadUrl))
                {
                    commandResponse.SetError("command", "Failed to get preSignedUrl");
                    _logger.LogError("Failed to get preSignedUrl");
                    return;
                }
                commandResponse.RequestUri = response?.UploadUrl;

                _logger.LogInformation("Writing storage service object={Response}", JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception for generated presigned url. Exception={message}", ex.Message);

                commandResponse.SetError("command", ex.Message);

                return;
            }

            _logger.LogInformation("Presigned url generated successfully!");
        }

        private async Task<ObjectArtifact> CreateFileArtifact(ObjectArtifactFileUploadCommand uploadFileCommand, Workspace workSpace)
        {
            string ownerName = _repository.GetItem<User>(u => u.ItemId == uploadFileCommand.UserId)?.DisplayName;
            var roles = new string[] { RoleNames.Admin };
            var ids = new string[] { uploadFileCommand.UserId };

            var objectArtifact = new ObjectArtifact
            {
                ItemId = uploadFileCommand.ObjectArtifactId ?? Guid.NewGuid().ToString(),
                FileStorageId = uploadFileCommand.FileStorageId,
                Name = uploadFileCommand.FileName,
                OwnerId = uploadFileCommand.UserId,
                OwnerName = ownerName,
                Description = uploadFileCommand.Description,
                ArtifactType = ArtifactTypeEnum.File,
                ParentId = uploadFileCommand.ParentId,
                StorageAreaId = uploadFileCommand.StorageAreaId,
                WorkSpaceId = uploadFileCommand.WorkspaceId,
                WorkSpaceName = workSpace.Name,
                Tags = uploadFileCommand.Tags,
                CreatedBy = uploadFileCommand.UserId,
                CreateDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                Language = "en-US",
                LastUpdatedBy = uploadFileCommand.UserId,
                MetaData = uploadFileCommand.MetaData,
                OrganizationId = uploadFileCommand?.OrganizationId,
                FileSizeInByte = uploadFileCommand.FileSizeInBytes,
                RolesAllowedToRead = roles,
                IdsAllowedToRead = ids,
                RolesAllowedToUpdate = roles,
                IdsAllowedToUpdate = ids,
                RolesAllowedToWrite = roles,
                IdsAllowedToWrite = ids,
                RolesAllowedToDelete = roles,
                IdsAllowedToDelete = ids
            };

            await _repository.SaveAsync(objectArtifact);

            return objectArtifact;
        }

        private async Task<bool> HasLicenseOrLimit(ObjectArtifactFileUploadCommand uploadFileCommand)
        {
            var licensingCommand = new GetLicensingSpecificationQuery
            {
                FeatureId = uploadFileCommand.FeatureId,
                OrganizationId = uploadFileCommand.OrganizationId
            };

            var licenseResponse = await _licensingService.GetLicensingSpecificationResponse(licensingCommand);

            if (!licenseResponse.UserHasLicense
                || licenseResponse.AvailableLimit < uploadFileCommand.FileSizeInBytes
                || uploadFileCommand.FileSizeInBytes <= 0)
            {
                _logger.LogError($"++Licensing error for FeatureId: {uploadFileCommand.FeatureId}, OrganizationId: {uploadFileCommand.OrganizationId}. Response:   {JsonConvert.SerializeObject(licenseResponse)}");
                return false;
            }

            return true;
        }

        public async Task<SearchResult> InitiateObjectArtifactRenameAsync(ObjectArtifactRenameCommand command)
        {
            SearchResult response = null;
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactSecuredById(command.ObjectArtifactId);

            if (objectArtifact != null)
            {
                bool isUpdated = await UpdateObjectArtifactAsync(command, objectArtifact);

                if (isUpdated)
                {
                    isUpdated &= await _storageDataService.RenameFileInCloud(objectArtifact.FileStorageId, command.Name);
                    if (isUpdated)
                    {
                        response = GetArtifactResponse(command);
                    }
                    PublishLibraryFileRenamedEvent(objectArtifact);
                }
            }

            return response;
        }

        private async Task<bool> UpdateObjectArtifactAsync(ObjectArtifactRenameCommand command, ObjectArtifact objectArtifact)
        {
            var updateDict = GetBaseUpdateDict();
            AddOptionalObjectArtifactUpdates(command, updateDict);
            var updateFilters = GetFilterById(command.ObjectArtifactId);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updateDict);
        }

        private Dictionary<string, object> GetBaseUpdateDict()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return new Dictionary<string, object>
            {
                { nameof(ObjectArtifact.LastUpdateDate), DateTime.UtcNow.ToLocalTime() },
                { nameof(ObjectArtifact.LastUpdatedBy), securityContext.UserId }
            };
        }

        private void AddOptionalObjectArtifactUpdates(ObjectArtifactRenameCommand command, Dictionary<string, object> updateDict)
        {
            if (!string.IsNullOrWhiteSpace(command.Name)) updateDict.Add(nameof(ObjectArtifact.Name), command.Name);
        }

        private FilterDefinition<BsonDocument> GetFilterById(string itemId)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", itemId);
        }

        private SearchResult GetArtifactResponse(ObjectArtifactRenameCommand command)
        {
            var objectArtifactSearchCommand = new ObjectArtifactSearchCommand()
            {
                ObjectArtifactId = command.ObjectArtifactId,
                Type = command.ViewMode
            };

            var artifactResponse = _objectArtifactSearchService.InitiateSearchObjectArtifact(objectArtifactSearchCommand);

            return artifactResponse;
        }

        private void PublishLibraryFileRenamedEvent(ObjectArtifact artifact)
        {
            var libraryFileRenamedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileRenamedEvent,
                JsonPayload = JsonConvert.SerializeObject(artifact.ItemId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryFileRenamedEvent);
        }

        private StorageArea GetStorageArea(string storageAreaId)
        {
            if (string.IsNullOrWhiteSpace(storageAreaId))
            {
                storageAreaId = DmsConstants.DefaultStorageAreaId;
                return new StorageArea()
                {
                    ItemId = storageAreaId
                };
            }
            var storageArea = this._repository.GetItem<StorageArea>(x => x.ItemId == storageAreaId);
            return storageArea;
        }

        private async Task<Workspace> GetWorkSpace(string workspaceId)
        {
            var workSpace = await _repository.GetItemAsync<Workspace>(x => x.ItemId == workspaceId);
            return workSpace;
        }
    }
}