using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactQueryService : IObjectArtifactQueryService
    {
        private readonly ILogger<ObjectArtifactQueryService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;

        public ObjectArtifactQueryService(
            ILogger<ObjectArtifactQueryService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
        }

        public async Task<List<ObjectArtifactSimpleResponse>> GetObjectArtifacts(ObjectArtifactQuery query)
        {
            var pipeline = BuildSearchPipeline(query);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();

            return PrepareRootFolderArtifactResponse(documents);
        }

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(ObjectArtifactQuery query)
        {
            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", PrepareMatchFilter(query)));
            BsonDocument sortDefinition = new BsonDocument("$sort", new BsonDocument($"{query.Sort.PropertyName}", query.Sort.Direction));
            BsonDocument skipDefinition = new BsonDocument("$skip", (query.PageNumber - 1) * query.PageSize);
            BsonDocument limitDefinition = new BsonDocument("$limit", query.PageSize);

            var pipelineDefinition = new BsonDocument[] { matchDefinition, sortDefinition, skipDefinition, limitDefinition };
            return pipelineDefinition;
        }

        private BsonArray PrepareMatchFilter(ObjectArtifactQuery query)
        {
            var matchFilter = new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareArtifactTypeWiseFilter(query.ArtifactType));

            var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));

            if (query.ArtifactType == ArtifactTypeEnum.Folder)
            {
                var removeOrgFolderFilter = _objectArtifactFilterUtilityService.RemoveOrganizationFolderFromFilter(query?.OrganizationId);
                if (removeOrgFolderFilter != null)
                {
                    matchFilter.Add(removeOrgFolderFilter);
                }
            }

            var roleWiseFilter = PrepareRoleWiseFilter(query.OrganizationId, query.DepartmentId);
            if (roleWiseFilter?.Count() > 0)
            {
                matchFilter.Add(roleWiseFilter);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                matchFilter
                    .Add(_objectArtifactFilterUtilityService.PrepareRegxTextSearchFilter(nameof(ObjectArtifact.Name), query.SearchText));
            }

            return matchFilter;
        }

        private BsonDocument PrepareRoleWiseFilter(string organizationId, string departmentId)
        {
            var matchFilter = !string.IsNullOrWhiteSpace(departmentId) ?
                _objectArtifactFilterUtilityService.PrepareDepartmentIdFilter(departmentId, organizationId, true) :
                !string.IsNullOrWhiteSpace(organizationId) ?
                _objectArtifactFilterUtilityService.PrepareOrganizationIdFilter(organizationId) : null;

            return matchFilter;
        }

        private List<ObjectArtifactSimpleResponse> PrepareRootFolderArtifactResponse(List<BsonDocument> documents)
        {
            var artifacts = new List<ObjectArtifactSimpleResponse>();

            documents.ForEach(document =>
            {
                var foundArtifact = BsonSerializer.Deserialize<ObjectArtifact>(document);

                var artifact = new ObjectArtifactSimpleResponse()
                {
                    ItemId = foundArtifact.ItemId,
                    Name = foundArtifact.Name,
                    ArtifactType = foundArtifact.ArtifactType,
                    Color = foundArtifact.Color,
                    IsSecretArtifact = _objectArtifactUtilityService.IsASecretArtifact(foundArtifact?.MetaData)
                };

                artifacts.Add(artifact);
            });

            return artifacts;
        }
    }
}