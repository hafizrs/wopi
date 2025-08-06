using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using System.Linq;
using Selise.Ecap.Entities;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryFolderSharedEventHandlerService : ILibraryFolderSharedEventHandlerService
    {
        private readonly ILogger<ObjectArtifactFolderShareService> _logger;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IObjectArtifactShareService _objectArtifactShareService;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;

        public const string childProp = "children";
        public const string dataProp = "data";

        public LibraryFolderSharedEventHandlerService(
            ILogger<ObjectArtifactFolderShareService> logger,
            IServiceClient serviceClient,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactShareService objectArtifactShareService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService)
        {
            _logger = logger;
            _serviceClient = serviceClient;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactShareService = objectArtifactShareService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
        }

        public async Task<bool> HandleObjectArtifactFolderSharedEvent(ObjectArtifactFileShareCommand command)
        {
            bool isShared = false;
            var objectArtifactId = command.ObjectArtifactId;
            var artifacts = await GetAllLinkedObjectArtifacts(objectArtifactId);
            if (artifacts != null && artifacts.Count > 0)
            {
                var children = artifacts.Where(a => a.ItemId != objectArtifactId)?.ToList() ?? new List<ObjectArtifact>();
                if (children.Count > 0)
                {
                    isShared = await InitiateSharebjectArtifactForAllChildren(children, command);
                    if (isShared)
                    {
                        PublishLibraryFolderTreeSharedEvent(artifacts.Select(a => a.ItemId).ToArray());
                    }
                }
            }

            return isShared;
        }

        private async Task<List<ObjectArtifact>> GetAllLinkedObjectArtifacts(string objectArtifactId)
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

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(string objectArtifactId)
        {
            var matchFilter =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareObjectArtifactIdFilter(objectArtifactId));

            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", matchFilter));
            BsonDocument graphLookUpDefinition = new BsonDocument("$graphLookup", new BsonDocument()
                                            .Add("from", $"{nameof(ObjectArtifact)}s")
                                            .Add("startWith", "$_id")
                                            .Add("connectFromField", "_id")
                                            .Add("connectToField", "ParentId")
                                            .Add("as", childProp)
                                            .Add("restrictSearchWithMatch", PrepareClildFindingFilter()));

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

        private BsonDocument PrepareClildFindingFilter()
        {
            var filters = new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter())
                .Add(PrepareArtifactTypeFilter());

            return new BsonDocument("$and", filters);
        }

        private BsonDocument PrepareArtifactTypeFilter()
        {
            var filters = new BsonArray();

            var folderFilter = _objectArtifactFilterUtilityService.PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.Folder);
            filters.Add(folderFilter);

            var fileFilter = PrepareApprovedFileExcludingGeneralFormFilter();
            filters.Add(fileFilter);

            var combinedFilter = new BsonDocument("$or", filters);

            return combinedFilter;
        }

        private BsonDocument PrepareApprovedFileExcludingGeneralFormFilter()
        {
            var filterValue =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareApprovedFileFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareExcludeGeneralFormFilter());
            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        public async Task<bool> InitiateSharebjectArtifactForAllChildren(List<ObjectArtifact> allChildren, ObjectArtifactFileShareCommand command)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            foreach (var child in allChildren)
            {
                listOfTasks.Add(_objectArtifactShareService.ShareObjectArtifact(child, command));
            }

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private void PublishLibraryFolderTreeSharedEvent(string[] objectArtifactIds)
        {
            var folderTreeSharedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFolderTreeSharedEvent,
                JsonPayload = JsonConvert.SerializeObject(objectArtifactIds)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), folderTreeSharedEvent);

            _logger.LogInformation(
                $"{PraxisEventType.LibraryFolderTreeSharedEvent} publiushed  with event:{JsonConvert.SerializeObject(folderTreeSharedEvent)}.");
        }
    }
}