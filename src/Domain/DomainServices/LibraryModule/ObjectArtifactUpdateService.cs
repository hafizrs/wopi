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
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactUpdateService : IObjectArtifactUpdateService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IChangeLogService _changeLogService;
        private readonly IRepository _repository;
        private readonly IDocumentKeywordService _documentKeywordService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public ObjectArtifactUpdateService(
            ISecurityContextProvider securityContextProvider,
            IChangeLogService changeLogService,
            IRepository repository,
            IDocumentKeywordService documentKeywordService,
            IObjectArtifactSearchService objectArtifactSearchService,
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
            _repository = repository;
            _documentKeywordService = documentKeywordService;
            _objectArtifactSearchService = objectArtifactSearchService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }

        public async Task<SearchResult> InitiateObjectArtifactUpdateAsync(ObjectArtifactUpdateCommand command)
        {
            SearchResult response = null;
            var objectArtifact = _objectArtifactUtilityService.GetEditableObjectArtifactById(command.ObjectArtifactId);

            if (objectArtifact != null)
            {
                bool isUpdated = await UpdateObjectArtifactAsync(command, objectArtifact);
                if (isUpdated)
                {
                    response = GetArtifactResponse(command);
                }
            }

            return response;
        }

        private async Task<bool> UpdateObjectArtifactAsync(ObjectArtifactUpdateCommand command, ObjectArtifact objectArtifact)
        {
            var updateDict = GetBaseUpdateDict();
            await AddOptionalObjectArtifactUpdates(command, objectArtifact, updateDict);

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

        private async Task AddOptionalObjectArtifactUpdates(ObjectArtifactUpdateCommand command, ObjectArtifact objectArtifact, Dictionary<string, object> updateDict)
        {
            if (command.Color != null) updateDict.Add(nameof(ObjectArtifact.Color), command.Color);

            await PrepareMetadataUpdates(command, objectArtifact, updateDict);
        }

        private FilterDefinition<BsonDocument> GetFilterById(string itemId)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", itemId);
        }

        private async Task PrepareMetadataUpdates(ObjectArtifactUpdateCommand command, ObjectArtifact objectArtifact, Dictionary<string, object> updateDict)
        {
            var stringDataType = LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[ObjectArtifactMetaDataKeyTypeEnum.STRING.ToString()];
            var metadataUpdateNeeded = false;

            if (command.Keywords?.Count > 0 || command.AreAllKeywordsRemoved == true)
            {
                var serializedKeywords = JsonConvert.SerializeObject(command.Keywords ?? new List<string>());
                ModifyObjectArtifactMetadataPair(objectArtifact.MetaData, ObjectArtifactMetaDataKeyEnum.KEYWORDS, stringDataType, serializedKeywords);
                metadataUpdateNeeded = true;

                if (command.Keywords?.Count > 0) await _documentKeywordService.UpdateKeywords(command.Keywords.ToArray(), command.ObjectArtifactId);
            }

            if (metadataUpdateNeeded)
            {
                updateDict.Add(nameof(ObjectArtifact.MetaData), objectArtifact.MetaData);
            }
        }

        private bool ModifyObjectArtifactMetadataPair(IDictionary<string, MetaValuePair> metaData, ObjectArtifactMetaDataKeyEnum keyEnum, string type, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var key = LibraryModuleConstants.ObjectArtifactMetaDataKeys[keyEnum.ToString()];
            var pair = new MetaValuePair { Type = type, Value = value };

            metaData ??= new Dictionary<string, MetaValuePair>();

            metaData[key] = pair;

            return true;
        }

        private SearchResult GetArtifactResponse(ObjectArtifactUpdateCommand command)
        {
            var objectArtifactSearchCommand = new ObjectArtifactSearchCommand()
            {
                ObjectArtifactId = command.ObjectArtifactId,
                Type = command.ViewMode
            };

            var artifactResponse = _objectArtifactSearchService.InitiateSearchObjectArtifact(objectArtifactSearchCommand);

            return artifactResponse;
        }

    }
}