using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Linq;
using MongoDB.Bson.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactActivationDeactivationService : IObjectArtifactActivationDeactivationService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IChangeLogService _changeLogService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;

        public ObjectArtifactActivationDeactivationService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IServiceClient serviceClient,
            IChangeLogService changeLogService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _serviceClient = serviceClient;
            _changeLogService = changeLogService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
        }

        public async Task<bool> InitiateObjectArtifactActivationDeactivationProcess(ObjectArtifactActivationDeactivationCommand command)
        {
            var objectArtifact = GetObjectArtifact(command.ObjectArtifactId);
            if (objectArtifact != null)
            {
                await UpdateObjectArtifact(objectArtifact, command.Activate);
                await UpdateChildObjectArtifact(objectArtifact, command.Activate);
                await _cockpitDocumentActivityMetricsGenerationService
                    .OnDocumentActivateDeactivateUpdateSummary(objectArtifact.ItemId, command.Activate);
            }
            return true;
        }

        private ObjectArtifact GetObjectArtifact(string id)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return _repository.GetItem<ObjectArtifact>(o =>
                o.ItemId == id &&
                !o.IsMarkedToDelete &&
                (o.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r)) || o.IdsAllowedToRead.Contains(securityContext.UserId)) &&
                (
                    (o.RolesAllowedToWrite != null && o.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r)))
                    || (o.IdsAllowedToWrite != null && o.IdsAllowedToWrite.Contains(securityContext.UserId))
                ));
        }

        private async Task<bool> UpdateObjectArtifact(ObjectArtifact objectArtifact, bool activate)
        {
            var update = PrepareObjectArtifactUpdate(objectArtifact, activate);
            if (update == null) return false;

            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", objectArtifact.ItemId);
            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, update);
        }

        private async Task UpdateChildObjectArtifact(ObjectArtifact objectArtifact, bool activate)
        {
            var childObjectArtifacts = await GetChildArtifacts(objectArtifact.ItemId);

            foreach (var childObjectArtifact in childObjectArtifacts)
            {
                await UpdateObjectArtifact(childObjectArtifact, activate);
            }
        }

        private Dictionary<string, object> PrepareObjectArtifactUpdate(ObjectArtifact objectArtifact, bool activate)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var metaDataUpdate = PrepareObjectArtifactMetaDataUpdate(objectArtifact.MetaData, activate);
            if (metaDataUpdate == null) return null;

            var updates = new Dictionary<string, object>
            {
                { "LastUpdateDate",  DateTime.UtcNow.ToLocalTime() },
                { "LastUpdatedBy", securityContext.UserId },
                { "MetaData", metaDataUpdate }
            };
            return updates;
        }

        private IDictionary<string, MetaValuePair> PrepareObjectArtifactMetaDataUpdate(IDictionary<string, MetaValuePair> metaData, bool activate)
        {
            metaData ??= new Dictionary<string, MetaValuePair>() { };

            var fileStatus = activate ?
                ((int)LibraryFileStatusEnum.ACTIVE).ToString() :
                ((int)LibraryFileStatusEnum.INACTIVE).ToString();
            var metaDataStatusValue = new MetaValuePair() { Type = "string", Value = fileStatus };
            var statusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];

            if (metaData.TryGetValue(statusKey, out MetaValuePair currentMetaDataStatusValue))
            {
                if (currentMetaDataStatusValue.Value == fileStatus) return null;
                metaData[statusKey] = metaDataStatusValue;
            }
            else
            {
                metaData.Add(statusKey, metaDataStatusValue);
            }

            return metaData;
        }
        private async Task<List<ObjectArtifact>> GetChildArtifacts(string parentId)
        {
            var originalArtifactIdKey = 
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID.ToString()];
            var fileTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.FILE_TYPE.ToString()];
            var isUploadedFromWebKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB.ToString()];
            var trueEnum = ((int)LibraryBooleanEnum.TRUE).ToString();
            var formTypeEnum = LibraryFileTypeEnum.FORM.ToString();
            var builder = Builders<ObjectArtifact>.Filter;

            var filter = builder.Exists(f => f.MetaData) & 
                         builder.Exists(f => f.MetaData[originalArtifactIdKey]) &
                         builder.Exists(f => f.MetaData[fileTypeKey]) &
                         builder.Eq(f => f.MetaData[originalArtifactIdKey].Value, parentId) &
                         builder.Eq(f => f.MetaData[fileTypeKey].Value, formTypeEnum) &
                         builder.Eq(f => f.IsMarkedToDelete, false) &
                         builder.Not(builder.Exists(f => f.MetaData[isUploadedFromWebKey]) & 
                                     builder.Eq(f => f.MetaData[isUploadedFromWebKey].Value, trueEnum));


            var projection = Builders<ObjectArtifact>.Projection
                .Include(f => f.ItemId)
                .Include(f => f.MetaData);
            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<ObjectArtifact>($"{nameof(ObjectArtifact)}s");
            var documents = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync();
            if (documents == null) return new List<ObjectArtifact>();
            var childArtifacts = documents
                .Select(d => BsonSerializer.Deserialize<ObjectArtifact>(d))
                .ToList();
            return childArtifacts;
        }
    }
}