using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using MongoDB.Bson.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSearchService : IObjectArtifactSearchService
    {
        private readonly ILogger<ObjectArtifactSearchService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactSearchQueryBuilderService _objectArtifactSearchQueryBuilderService;
        private readonly IObjectArtifactSearchResponseGeneratorService _objectArtifactSearchResponseGeneratorService;

        public ObjectArtifactSearchService(
            ILogger<ObjectArtifactSearchService> logger,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISecurityHelperService securityHelperService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactSearchQueryBuilderService objectArtifactSearchQueryBuilderService,
            IObjectArtifactSearchResponseGeneratorService objectArtifactSearchResponseGeneratorService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _securityHelperService = securityHelperService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactSearchQueryBuilderService = objectArtifactSearchQueryBuilderService;
            _objectArtifactSearchResponseGeneratorService = objectArtifactSearchResponseGeneratorService;
        }

        public SearchResult InitiateSearchObjectArtifact(ObjectArtifactSearchCommand command)
        {
            UpdateSearchObjectArtifactCommand(command);
            var artifacts = SearchObjectArtifact(command).Result;
            var result = new SearchResult(artifacts, null);
            result.Count = result.Data.Count();

            return result;
        }

        private void UpdateSearchObjectArtifactCommand(ObjectArtifactSearchCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Type))
            {
                command.Type = _objectArtifactUtilityService.GetLibraryViewModeName($"{LibraryViewModeEnum.ALL}");
            }
            if (string.IsNullOrWhiteSpace(command.ParentId))
            {
                command.ParentId = null;
            }
        }

        private async Task<List<IDictionary<string, object>>> SearchObjectArtifact(ObjectArtifactSearchCommand command)
        {
            var pipeline = _objectArtifactSearchQueryBuilderService.BuildSearchPipeline(command);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();

            var results = _objectArtifactSearchResponseGeneratorService.PrepareArtifactResponse(documents, command);

            return results;
        }
    }
}