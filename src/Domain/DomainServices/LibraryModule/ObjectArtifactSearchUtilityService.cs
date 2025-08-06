using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSearchUtilityService : IObjectArtifactSearchUtilityService
    {
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;
        private readonly ISecurityHelperService _securityHelperService;

        public const string childProp = "children";
        public const string dataProp = "data";

        public ObjectArtifactSearchUtilityService(
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService,
            ISecurityHelperService securityHelperService
        )
        {
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
            _securityHelperService = securityHelperService;
        }

        public async Task<List<ObjectArtifact>> GetParentWithAllChildrenObjectArtifacts(string objectArtifactId)
        {
            var allChilds = new List<ObjectArtifact>();
            var pipeline = BuildSearchPipeline(objectArtifactId);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();

            foreach (var document in documents)
            {
                document.TryGetValue(dataProp, out BsonValue value);
                if (value != null)
                {
                    var foundArtifact = BsonSerializer.Deserialize<ObjectArtifact>(value.AsBsonDocument);
                    allChilds.Add(foundArtifact);
                }
            }

            return allChilds;
        }

        public async Task<List<ObjectArtifact>> GetAllChildrenObjectArtifacts(string objectArtifactId)
        {
            var allChilds = new List<ObjectArtifact>();
            var pipeline = BuildSearchPipeline(objectArtifactId);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();

            foreach (var document in documents)
            {
                document.TryGetValue(dataProp, out BsonValue value);
                if (value != null)
                {
                    var foundArtifact = BsonSerializer.Deserialize<ObjectArtifact>(value.AsBsonDocument);
                    if (foundArtifact?.ItemId != objectArtifactId)
                    {
                        allChilds.Add(foundArtifact);
                    }
                }
            }

            return allChilds;
        }

        public BsonDocument PrepareRegExTextSearchFilter(string propertyName, string inputText)
        {
            return 
                new BsonDocument(
                    propertyName,
                    new BsonRegularExpression($"{inputText}", "i")); ;
        }

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(string objectArtifactId)
        {
            var matchFilter =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareObjectArtifactIdFilter(objectArtifactId))
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter());

            var deptId = _securityHelperService.IsADepartmentLevelUser() ? 
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));
            

            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", matchFilter));
            BsonDocument graphLookUpDefinition = new BsonDocument("$graphLookup", new BsonDocument()
                                            .Add("from", $"{nameof(ObjectArtifact)}s")
                                            .Add("startWith", "$_id")
                                            .Add("connectFromField", "_id")
                                            .Add("connectToField", "ParentId")
                                            .Add("as", childProp)
                                            .Add("restrictSearchWithMatch", _objectArtifactFilterUtilityService.PrepareClildFindingFilter()));

            BsonDocument projecttionDefinition1 = new BsonDocument("$project", new BsonDocument()
                                            .Add("data", "$$ROOT"));

            BsonDocument projecttionDefinition2 = new BsonDocument("$project", new BsonDocument()
                                            .Add("data", new BsonDocument()
                                                    .Add("$reduce", new BsonDocument()
                                                            .Add("input", new BsonArray()
                                                                    .Add("$data")
                                                            )
                                                            .Add("initialValue", $"$data.{childProp}")
                                                            .Add("in", new BsonDocument()
                                                                    .Add("$concatArrays", new BsonArray()
                                                                            .Add("$$value")
                                                                            .Add(new BsonArray()
                                                                                    .Add("$$this")
                                                                            )
                                                                    )
                                                            )
                                                    )
                                            ));

            BsonDocument unwindDefinition = new BsonDocument("$unwind", "$data");

            var pipelineDefinition = new BsonDocument[]
            {
                matchDefinition, graphLookUpDefinition, projecttionDefinition1, projecttionDefinition2, unwindDefinition
            };

            return pipelineDefinition;
        }
    }
}