using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DmsArtifactUsageReferenceEventHandlerService : IDmsArtifactUsageReferenceEventHandlerService
    {
        private readonly ILogger<DmsArtifactUsageReferenceEventHandlerService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;

        public DmsArtifactUsageReferenceEventHandlerService(
            ILogger<DmsArtifactUsageReferenceEventHandlerService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
        }
        public async Task InitiateArtifactUsageReferenceCreation(DmsArtifactUsageReferenceEventModel payload)
        {
            _logger.LogInformation("Entered in {ServiceName} with payload: {Payload}", nameof(DmsArtifactUsageReferenceEventHandlerService), JsonConvert.SerializeObject(payload));
            try
            {
                var artifacts = _objectArtifactUtilityService.GetObjectArtifacts(payload.ObjectArtifactIds.ToArray());
                var currentUserId = _securityContextProvider.GetSecurityContext().UserId;
                var praxisUser = _objectArtifactUtilityService.GetPraxisUserByUserId(currentUserId);
                
                var existingReference = await GetProjectedDmsArtifactReference(payload.RelatedEntityId, payload.RelatedEntityName);

                await FilterOutArtifactsWithExistingReferenceAndUpdateArtifacts(artifacts, existingReference, payload);

                var taskList = new List<Task>();
                taskList.AddRange(artifacts.Select(artifact => CreateUsageReference(artifact, payload, currentUserId, praxisUser)));
                taskList.Add(UpdateObjectArtifactsCounter(artifacts, 1));
                taskList.AddRange(existingReference.Select(artifact => UpdateExistingReference(artifact, payload, currentUserId)));


                foreach (var batch in taskList.Chunk(100))
                {
                    await Task.WhenAll(batch);
                }

            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {ServiceName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(DmsArtifactUsageReferenceEventHandlerService), JsonConvert.SerializeObject(payload), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled by: {ServiceName}.", nameof(DmsArtifactUsageReferenceEventHandlerService));
        }
        public async Task InitiateArtifactUsageReferenceDeletion(DmsArtifactUsageReferenceDeleteEventModel payload)
        {
            _logger.LogInformation("Entered in {ServiceName} with   payload: {Payload}", nameof(DmsArtifactUsageReferenceEventHandlerService), JsonConvert.SerializeObject(payload));
            try
            {
                var taskList = new List<Task>
                {
                    UpdateExistingReferenceToDelete(payload.ObjectArtifactIds, payload.RelatedEntityId),
                    UpdateObjectArtifactsCounter(payload.ObjectArtifactIds, -1)
                };
                await Task.WhenAll(taskList);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {ServiceName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(DmsArtifactUsageReferenceEventHandlerService), JsonConvert.SerializeObject(payload), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled by: {ServiceName}.", nameof(DmsArtifactUsageReferenceEventHandlerService));
        }
        private async Task CreateUsageReference(ObjectArtifact artifact, DmsArtifactUsageReferenceEventModel payload, string currentUserId, PraxisUser praxisUser)
        {
            var clientInfos = payload.ClientInfos;
            var org = payload.OrganizationId;
            var orgIds = payload.OrganizationIds ?? new List<string>();
            if (payload.RelatedEntityName == nameof(PraxisForm))
            {
                var form = _repository.GetItem<PraxisForm>(f => f.ItemId.Equals(payload.RelatedEntityId));
                (orgIds, clientInfos) = GetOrgAndClientsOfForm(form, artifact.ItemId);
            }
            var usageReference = new DmsArtifactUsageReference
            {
                ItemId = Guid.NewGuid().ToString(),
                ObjectArtifactId = artifact.ItemId,
                RelatedEntityId = payload.RelatedEntityId,
                RelatedEntityName = payload.RelatedEntityName,
                PurposeEntityName = payload.PurposeEntityName,
                Title = payload.Title,
                AttachmentAssignedBy = praxisUser?.ItemId ?? string.Empty,
                IsTaskCompleted = false,
                IdsAllowedToRead = artifact.IdsAllowedToRead,
                RolesAllowedToRead = artifact.RolesAllowedToRead,
                CreateDate = DateTime.UtcNow,
                CreatedBy = currentUserId,
                LastUpdatedBy = currentUserId,
                LastUpdateDate = DateTime.UtcNow,
                TaskCompletionInfo = new RelatedTaskCompletionInfo
                {
                    DueDate = payload.DueDate,
                    CompletionStatus = payload.CompletionStatus
                },
                MetaData = payload.MetaData,
                ClientInfos = clientInfos,
                OrganizationId = org,
                OrganizationIds = orgIds
            };
            await _repository.SaveAsync(usageReference);
        }

        private async Task UpdateExistingReference(DmsArtifactUsageReference reference, DmsArtifactUsageReferenceEventModel payload,
            string currentUserId)
        {
            var clientInfos = payload.ClientInfos;
            var org = payload.OrganizationId;
            var orgIds = payload.OrganizationIds;
            if (payload.RelatedEntityName == nameof(PraxisForm))
            {
                var form = _repository.GetItem<PraxisForm>(f => f.ItemId.Equals(payload.RelatedEntityId));
                (orgIds, clientInfos) = GetOrgAndClientsOfForm(form, reference.ObjectArtifactId);
            }

            reference.ClientInfos = clientInfos;
            reference.OrganizationId = org;
            reference.OrganizationIds = orgIds;
            reference.MetaData = payload.MetaData;
            reference.LastUpdatedBy = currentUserId;
            reference.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
            await _repository.UpdateAsync(r => r.ItemId == reference.ItemId, reference);
        }
        private (List<string>, List<FormSpecificClientInfo>) GetOrgAndClientsOfForm(PraxisForm form, string objectArtifactId)
        {
            if (form.PurposeOfFormKey == "process-guide")
            {
                var organizationIds = form.ProcessGuideCheckList
                    .Where(p => p.ProcessGuideTask.Any(gt => 
                        gt.Files?.Any(f => f.DocumentId == objectArtifactId) == true ||
                        gt.LibraryForms?.Any(f => f.LibraryFormId == objectArtifactId) == true))
                    .Where(f => f.OrganizationIds != null)
                    .SelectMany(f => f.OrganizationIds)
                    .ToList();
                var clientInfos = form.ProcessGuideCheckList
                    .Where(p => p.ProcessGuideTask.Any(gt => 
                        gt.Files?.Any(f => f.DocumentId == objectArtifactId) == true ||
                        gt.LibraryForms?.Any(f => f.LibraryFormId == objectArtifactId) == true))
                    .Where(f => f.ClientInfos != null)
                    .SelectMany(f => f.ClientInfos)
                    .ToList();
                return (organizationIds, clientInfos);
            }

            if (form.PurposeOfFormKey == "training-module")
            {
                return (form.OrganizationIds, form.ClientInfos?.ToList());
            }

            return (null, null);
        }
        private async Task FilterOutArtifactsWithExistingReferenceAndUpdateArtifacts(
            List<ObjectArtifact> artifacts, 
            List<DmsArtifactUsageReference> existingReference,
            DmsArtifactUsageReferenceEventModel payload)
        {
            if (existingReference == null || !existingReference.Any()) return;

            var existingReferenceArtifactIds = existingReference
                .Select(r => r.ObjectArtifactId)
                .ToHashSet();
            var referencesToBeSkipped = existingReferenceArtifactIds
                .Intersect(artifacts.Select(a => a.ItemId))
                .ToHashSet();

            // Remove artifacts that have existing references and gather IDs for those to be marked for deletion
            var artifactsToUpdate = referencesToBeSkipped.ToList();
            artifacts.RemoveAll(a => referencesToBeSkipped.Contains(a.ItemId));
            var markedToDeleteArtifactIds = existingReferenceArtifactIds
                .Except(referencesToBeSkipped)
                .ToList();

            var updateTasks = new List<Task>
            {
                UpdateExistingReferenceToDelete(markedToDeleteArtifactIds, payload.RelatedEntityId),
                UpdateExistingReferenceCompletionStatus(artifactsToUpdate, new RelatedTaskCompletionInfo
                {
                    DueDate = payload.DueDate, CompletionStatus = payload.CompletionStatus
                }, payload.RelatedEntityId),
                UpdateObjectArtifactsCounter(markedToDeleteArtifactIds, -1),
            };

            await Task.WhenAll(updateTasks);
        }
        private async Task UpdateExistingReferenceToDelete(List<string> artifactIds, string relatedEntityId)
        {
            var updates = GetCommonPropertiesToUpdate();
            updates.Add(nameof(DmsArtifactUsageReference.IsMarkedToDelete), true);
            await _repository.UpdateAsync<DmsArtifactUsageReference>(r => 
                artifactIds.Contains(r.ObjectArtifactId) && r.RelatedEntityId.Equals(relatedEntityId), updates);
        }
        private async Task UpdateExistingReferenceCompletionStatus(List<string> artifactIds, RelatedTaskCompletionInfo info, string relatedEntityId)
        {
            var updates = GetCommonPropertiesToUpdate();
            updates.Add(nameof(DmsArtifactUsageReference.TaskCompletionInfo), info?.ToBsonDocument());
            await _repository.UpdateAsync<DmsArtifactUsageReference>(r => 
                artifactIds.Contains(r.ObjectArtifactId) && r.RelatedEntityId.Equals(relatedEntityId), updates);
        }
        private async Task UpdateObjectArtifactsCounter(List<string> referenceIds, int offset = 0)
        {
            var artifacts = _repository
                .GetItems<ObjectArtifact>(a => referenceIds.Contains(a.ItemId))?
                .ToList() ?? new List<ObjectArtifact>();
            await UpdateObjectArtifactsCounter(artifacts, offset);
        }
        private async Task UpdateObjectArtifactsCounter(List<ObjectArtifact> artifacts, int offset = 0)
        {
            var taskList = artifacts
                .Select(a => UpdateArtifactCounter(a, offset))
                .ToList();

            foreach (var batch in taskList.Chunk(100))
            {
                await Task.WhenAll(batch);
            }
        }
        private async Task UpdateArtifactCounter(ObjectArtifact artifact, int offset = 0)
        {
            var updates = GetCommonPropertiesToUpdate();
            artifact.MetaData ??= new Dictionary<string, MetaValuePair>();

            var artifactUsageReferenceCounterKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                nameof(ObjectArtifactMetaDataKeyEnum.ARTIFACT_USAGE_REFERENCE_COUNTER)];
            var isUsedInAnotherEntityKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                nameof(ObjectArtifactMetaDataKeyEnum.IS_USED_IN_ANOTHER_ENTITY)];

            if (!artifact.MetaData.TryGetValue(artifactUsageReferenceCounterKey, out var counterValue))
            {
                counterValue = new MetaValuePair { Value = "0", Type = "string" };
                artifact.MetaData[artifactUsageReferenceCounterKey] = counterValue;
            }

            var counter = int.TryParse(counterValue.Value, out var cnt) ? cnt : 0;
            counter = Math.Max(0, counter + offset);

            artifact.MetaData[isUsedInAnotherEntityKey] = new MetaValuePair
            {
                Value = counter == 0 
                    ? ((int)LibraryBooleanEnum.FALSE).ToString() 
                    : ((int)LibraryBooleanEnum.TRUE).ToString(),
                Type = "string"
            };

            counterValue.Value = counter.ToString();

            updates[nameof(ObjectArtifact.MetaData)] = artifact.MetaData.ToBsonDocument();
            await _repository.UpdateAsync<ObjectArtifact>(a => a.ItemId.Equals(artifact.ItemId), updates);
        }
        private Dictionary<string, object> GetCommonPropertiesToUpdate()
        {
            var localTime = DateTime.UtcNow.ToLocalTime();
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var updates = new Dictionary<string, object>
            {
                { nameof(EntityBase.LastUpdatedBy), userId },
                { nameof(EntityBase.LastUpdateDate), localTime }
            };
            return updates;
        }
        private async Task<List<DmsArtifactUsageReference>> GetProjectedDmsArtifactReference(string relatedEntityId, string relatedEntityName)
        {
            var builder = Builders<DmsArtifactUsageReference>.Filter;
            var filter = builder.Eq(r => r.RelatedEntityId, relatedEntityId) &
                         builder.Eq(r => r.RelatedEntityName, relatedEntityName) &
                         builder.Eq(r => r.IsMarkedToDelete, false);
            
            var projection = Builders<DmsArtifactUsageReference>.Projection
                .Include(r => r.ObjectArtifactId);
            
            var collection = _mongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<DmsArtifactUsageReference>($"{EntityName.DmsArtifactUsageReference}s");
            
            var documents = await collection
                .Find(filter)
                .ToListAsync();
            return documents?.ToList() ?? new List<DmsArtifactUsageReference>();
        }
    }
}