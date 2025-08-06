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
using System.Linq;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactMoveService : IObjectArtifactMoveService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IChangeLogService _changeLogService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IServiceClient _serviceClient;

        public ObjectArtifactMoveService(
            ISecurityContextProvider securityContextProvider,
            IChangeLogService changeLogService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IServiceClient serviceClient,
            IObjectArtifactShareService objectArtifactShareService
        )
        {
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _serviceClient = serviceClient;
            _objectArtifactShareService = objectArtifactShareService;
        }

        public async Task<bool> InitiateObjectArtifactMoveAsync(ObjectArtifactMoveCommand command)
        {
            try
            {
                var taskList = command.ObjectArtifactIds?
                    .Select(async objectArtifactId => 
                    { 
                        // Get child artifact
                        var childArtifact = _objectArtifactUtilityService.GetWritableObjectArtifactById(objectArtifactId);

                        // Get parent artifact
                        var parentArtifact = !string.IsNullOrWhiteSpace(command.NewParentId)
                            ? _objectArtifactUtilityService.GetWritableObjectArtifactById(command.NewParentId)
                            : null;

                        if (!string.IsNullOrEmpty(command.NewParentId) && parentArtifact == null || childArtifact == null)
                        {
                            throw new UnauthorizedAccessException("You do dot have permission to move");
                        }

                        // Move artifact asynchronously
                        var result = await MoveObjectArtifactAsync(childArtifact, parentArtifact);

                        // Publish event after move
                        PublishLibraryFileMovedEvent(objectArtifactId);

                        return result;
                    }).ToList() ?? new List<Task<bool>>();

                var response = await Task.WhenAll(taskList);
                return response.All(r => r);
            }
            catch (Exception e)
            {
                throw new Exception("Error occurred while moving object artifact", e);
            }
        }

        public async Task MoveChildObjectArtifactsAsync(string parentId, List<string> artifactIds)
        {
            if (artifactIds.Contains(parentId)) return;
            artifactIds.Add(parentId);

            var parentArtifact = _objectArtifactUtilityService.GetObjectArtifactById(parentId);
            var childArtifacts = _objectArtifactUtilityService.GetObjectArtifactsByParentId(parentId);

            foreach (var childArtifact in childArtifacts)
            {
                _ = await MoveObjectArtifactAsync(childArtifact, parentArtifact);
                if (childArtifact.ArtifactType == ArtifactTypeEnum.Folder)
                {
                    await MoveChildObjectArtifactsAsync(childArtifact.ItemId, artifactIds);
                }
                else
                {
                    artifactIds.Add(childArtifact.ItemId);
                }
            }
        }

        private void UpdateObjectArtifactMoveCommand(ObjectArtifactMoveCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.NewParentId))
            {
                command.NewParentId = null;
            }
        }

        private async Task<bool> MoveObjectArtifactAsync(ObjectArtifact childArtifact, ObjectArtifact parentArtifact)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>
            {
                UpdateChildObjectArtifactAsync(childArtifact, parentArtifact)
            };

            //if (parentArtifact != null)
            //{
            //    var command = _objectArtifactShareService.GetCurrentAccessControl(parentArtifact);
            //    ResetAllPermission(childArtifact, parentArtifact);
            //    listOfTasks.Add(
            //        _objectArtifactShareService.ShareObjectArtifact(childArtifact, command)
            //    );
            //}
            var response = await Task.WhenAll(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private void ResetAllPermission(ObjectArtifact artifact, ObjectArtifact parentArtifact, Dictionary<string, object> updates)
        {
            var clonedParentArtifact = JsonConvert.DeserializeObject<ObjectArtifact>(JsonConvert.SerializeObject(parentArtifact));

            var isAForm = _objectArtifactUtilityService.IsAForm(artifact.MetaData);

            if (isAForm)
            {
                foreach (var item in clonedParentArtifact.SharedOrganizationList ?? new List<SharedOrganizationInfo>())
                {
                    item.FeatureName = item.FeatureName == "update" ? "form_fill" : item.FeatureName;
                }
            }

            var emptyArray = new List<string>();
            artifact.SharedOrganizationList = clonedParentArtifact?.SharedOrganizationList ?? new List<SharedOrganizationInfo>();
            artifact.SharedPersonIdList = clonedParentArtifact?.SharedPersonIdList ?? emptyArray;
            artifact.SharedUserIdList = clonedParentArtifact?.SharedUserIdList ?? emptyArray;
            artifact.SharedRoleList = clonedParentArtifact?.SharedRoleList ?? emptyArray;
            artifact.RolesAllowedToRead = clonedParentArtifact?.RolesAllowedToRead ?? emptyArray.ToArray();
            artifact.IdsAllowedToRead = clonedParentArtifact?.IdsAllowedToRead ?? emptyArray.ToArray();
            artifact.RolesAllowedToUpdate = clonedParentArtifact?.RolesAllowedToUpdate ?? emptyArray.ToArray();
            artifact.IdsAllowedToUpdate = clonedParentArtifact?.IdsAllowedToUpdate ?? emptyArray.ToArray();

            updates[nameof(ObjectArtifact.SharedOrganizationList)] = artifact.SharedOrganizationList;
            updates[nameof(ObjectArtifact.SharedPersonIdList)] = artifact.SharedPersonIdList;
            updates[nameof(ObjectArtifact.SharedUserIdList)] = artifact.SharedUserIdList;
            updates[nameof(ObjectArtifact.SharedRoleList)] = artifact.SharedRoleList;
            updates[nameof(ObjectArtifact.RolesAllowedToRead)] = artifact.RolesAllowedToRead;
            updates[nameof(ObjectArtifact.IdsAllowedToRead)] = artifact.IdsAllowedToRead;
            updates[nameof(ObjectArtifact.RolesAllowedToUpdate)] = artifact.RolesAllowedToUpdate;
            updates[nameof(ObjectArtifact.IdsAllowedToUpdate)] = artifact.IdsAllowedToUpdate;
        }

        private async Task<bool> UpdateChildObjectArtifactAsync(ObjectArtifact childArtifact, ObjectArtifact parentArtifact)
        {
            var updateFilter = GetFilterById(childArtifact.ItemId);
            var updates = PrepareChildObjectArtifactUpdates(childArtifact, parentArtifact);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilter, updates);
        }

        private Dictionary<string, object> PrepareChildObjectArtifactUpdates(ObjectArtifact childArtifact, ObjectArtifact parentArtifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            childArtifact.LastUpdateDate = DateTime.UtcNow;
            childArtifact.LastUpdatedBy = securityContext.UserId;
            childArtifact.ParentId = parentArtifact?.ItemId;
            _ = ModifySecretArtifactMetaData(childArtifact, parentArtifact);

            var updates = new Dictionary<string, object>
            {
                { nameof(ObjectArtifact.LastUpdateDate), childArtifact.LastUpdateDate },
                { nameof(ObjectArtifact.LastUpdatedBy), childArtifact.LastUpdatedBy },
                { nameof(ObjectArtifact.ParentId), childArtifact.ParentId },
                { nameof(ObjectArtifact.MetaData), _objectArtifactShareService.PrepareObjectArtifactMetaDataUpdate(childArtifact.MetaData, DateTime.UtcNow) }
            };

            if (parentArtifact != null)
            {
                ResetAllPermission(childArtifact, parentArtifact, updates);
            }

            return updates;
        }

        private bool ModifySecretArtifactMetaData(ObjectArtifact childArtifact, ObjectArtifact parentArtifact)
        {
            if (childArtifact?.MetaData == null) return false;
            if (
                _objectArtifactUtilityService.IsASecretArtifact(parentArtifact?.MetaData) &&
                !_objectArtifactUtilityService.IsASecretArtifact(childArtifact?.MetaData)
            )
            {
                var isSecretKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT.ToString()];
                childArtifact.MetaData.Add
                (
                    isSecretKey,
                    new MetaValuePair { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() }
                );

                return true;
            }
            if (
                !_objectArtifactUtilityService.IsASecretArtifact(parentArtifact?.MetaData) &&
                _objectArtifactUtilityService.IsASecretArtifact(childArtifact?.MetaData)
            )
            {
                var isSecretKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT.ToString()];
                childArtifact.MetaData.Remove(isSecretKey);

                return true;
            }

            return false;
        }
        
        private FilterDefinition<BsonDocument> GetFilterById(string itemId)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", itemId);
        }

        private void PublishLibraryFileMovedEvent(string objectArtifactId)
        {
            var fileMovedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFileMovedEvent,
                JsonPayload = JsonConvert.SerializeObject(objectArtifactId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), fileMovedEvent);
        }
    }
}