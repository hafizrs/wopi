using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFormHistoryService : IObjectArtifactFormHistoryService
    {
        private readonly ILogger<ObjectArtifactFormHistoryService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactSearchResponseGeneratorService _objectArtifactSearchResponseGeneratorService;

        public ObjectArtifactFormHistoryService(
            ILogger<ObjectArtifactFormHistoryService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService,
            IObjectArtifactSearchResponseGeneratorService objectArtifactSearchResponseGeneratorService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
            _objectArtifactSearchResponseGeneratorService = objectArtifactSearchResponseGeneratorService;
        }

        public async Task<List<IDictionary<string, object>>> GetObjectArtifactFormHistory(ObjectArtifactFormHistoryQuery query)
        {
            var pipeline = BuildSearchPipeline(query);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();

            var formResponses = documents
                    .Select(v => BsonSerializer.Deserialize<RiqsObjectArtifact>(v.AsBsonDocument))
                    .ToList();

            var entityDictionary = await _objectArtifactSearchResponseGeneratorService.GetDependentArtifactResponseProperties(documents);
            return _objectArtifactSearchResponseGeneratorService.PrepareFormResponseArtifactResponse(formResponses, false, entityDictionary);
        }

        public PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(ObjectArtifactFormHistoryQuery query)
        {
            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", PrepareMatchFilter(query)));
            BsonDocument sortDefinition = new BsonDocument("$sort", new BsonDocument($"{query.Sort.PropertyName}", query.Sort.Direction));
            BsonDocument skipDefinition = new BsonDocument("$skip", (query.PageNumber - 1) * query.PageSize);
            BsonDocument limitDefinition = new BsonDocument("$limit", query.PageSize);

            var pipelineDefinition = new BsonDocument[] { matchDefinition, sortDefinition, skipDefinition, limitDefinition };

            return pipelineDefinition;
        }

        private BsonArray PrepareMatchFilter(ObjectArtifactFormHistoryQuery query)
        {
            var matchFilter = new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareChildArtifactFilter(query.ObjectArtifactId))
                .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter());

            var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));

            var textSearchFilter = _objectArtifactFilterUtilityService.PrepareObjectArtifactTextSearchFilter(query.SearchText);
            if (textSearchFilter != null)
            {
                matchFilter.Add(textSearchFilter);
            }

            return matchFilter;
        }


    }
}