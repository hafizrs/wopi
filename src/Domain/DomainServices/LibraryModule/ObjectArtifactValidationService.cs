using System;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Microsoft.Extensions.Logging;
using System.Linq;
using Selise.Ecap.Entities;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactValidationService : IObjectArtifactValidationService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly ILogger<ObjectArtifactValidationService> _logger;

        public ObjectArtifactValidationService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            ILogger<ObjectArtifactValidationService> logger
        )
        {
            _securityContextProvider = securityContextProvider;
            _repository = repository;
            _logger = logger;
        }

        public bool ValidateCreateFolder(ObjectArtifactFolderCreateCommand createFolderCommand, CommandResponse commandResponse, Workspace workSpace, StorageArea storageArea, bool autoValid = false)
        {
            ValidateStorageArea(storageArea, commandResponse, null, ArtifactTypeEnum.Folder);
            ValidateWorkspace(createFolderCommand.UserId, workSpace, commandResponse);
            ValidateParentId(createFolderCommand.ParentId, createFolderCommand.UserId, commandResponse, autoValid);
            ValidateObjectArtifact(createFolderCommand.ObjectArtifactId, commandResponse);

            return commandResponse.Errors.IsValid;
        }

        public bool ValidateUploadFile(ObjectArtifactFileUploadCommand uploadFileCommand, CommandResponse commandResponse, Workspace workSpace, StorageArea storageArea)
        {
            ValidateStorageArea(storageArea, commandResponse, uploadFileCommand.FileStorageId, ArtifactTypeEnum.File);
            ValidateWorkspace(uploadFileCommand.UserId, workSpace, commandResponse);
            ValidateParentId(uploadFileCommand.ParentId, uploadFileCommand.UserId, commandResponse);
            ValidateObjectArtifact(uploadFileCommand.ObjectArtifactId, commandResponse);

            return commandResponse.Errors.IsValid;
        }

        private void ValidateStorageArea(StorageArea storageArea, CommandResponse commandResponse, string fileId, ArtifactTypeEnum artifactType)
        {
            if (storageArea == null)
            {
                LogAndAddValidationError("Storage area not available.", "Storage area not available.", commandResponse);
            }
            else if (string.IsNullOrEmpty(fileId) && artifactType == ArtifactTypeEnum.File)
            {
                LogAndAddValidationError("File storage id can't be empty.", "File storage id can't be empty.", commandResponse);
            }
        }

        private void ValidateWorkspace(string userId, Workspace workSpace, CommandResponse commandResponse)
        {
            if (workSpace == null || (!IsWorkspaceOwner(userId, workSpace) && !ValidateWorkspaceRolesAndIdsForUploadFileCommand(userId, workSpace)))
            {
                LogAndAddValidationError("Invalid workspace id specified. File can't be uploaded.", "Invalid workspace id specified. File can't be uploaded.", commandResponse);
            }
        }

        private void ValidateParentId(string parentId, string userId, CommandResponse commandResponse, bool autoValid = false)
        {
            if (autoValid) return;
            if (parentId != null)
            {
                var parentObjectArtifact = GetParentObjectArtifact(parentId);
                if (parentObjectArtifact == null || (!ValidateParentObjectArtifactRolesAndIds(userId, parentObjectArtifact)))
                {
                    LogAndAddValidationError("Invalid parent id specified. Parameter ParentId does not match anything", "Invalid parent id specified", commandResponse);
                }
            }
        }

        private void ValidateObjectArtifact(string artifactId, CommandResponse commandResponse)
        {
            if (artifactId == null) return;
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                LogAndAddValidationError("ArtifactId cannot be empty string.", "ArtifactId cannot be empty string.", commandResponse);
                return;
            }

            var artifact = _repository.GetItem<ObjectArtifact>(x => x.ItemId == artifactId);
            if (artifact != null)
            {
                LogAndAddValidationError("Artifact already exists for this object artifact id.", "Artifact already exists for this object artifact id.", commandResponse);
            }
        }

        private void LogAndAddValidationError(string infoMessage, string errorMessage = null, CommandResponse commandResponse = null)
        {
            _logger.LogInformation(infoMessage);
            commandResponse.SetError("command", !string.IsNullOrEmpty(errorMessage) ? errorMessage : infoMessage);
        }

        private ObjectArtifact GetParentObjectArtifact(string parentId)
        {
            return parentId != null ? _repository.GetItem<ObjectArtifact>(x => x.ItemId == parentId) : null;
        }

        private bool IsWorkspaceOwner(string userId, Workspace workSpace)
        {
            return workSpace.OwnerId == userId;
        }

        private bool ValidateParentObjectArtifactRolesAndIds(string userId, ObjectArtifact parentObjectArtifact)
        {
            if (parentObjectArtifact?.ItemId == parentObjectArtifact?.OrganizationId) return true;

            var roles = _securityContextProvider.GetSecurityContext().Roles;
            return (parentObjectArtifact.RolesAllowedToRead != null && parentObjectArtifact.RolesAllowedToRead.Intersect(roles).Any()) ||
                   (parentObjectArtifact.IdsAllowedToRead != null && parentObjectArtifact.IdsAllowedToRead.Contains(userId));
        }

        private bool ValidateWorkspaceRolesAndIdsForUploadFileCommand(string userId, Workspace workSpace)
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            return (workSpace.RolesAllowedToRead != null && workSpace.RolesAllowedToRead.Intersect(roles).Any()) ||
                   (workSpace.IdsAllowedToRead != null && workSpace.IdsAllowedToRead.Contains(userId));
        }
    }
 }