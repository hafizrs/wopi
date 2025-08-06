using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactSearchQueryBuilderService
    {
        PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(ObjectArtifactSearchCommand command);
        BsonDocument PrepareRoleWiseFilter(string organizationId, string departmentId, string parentId);
        BsonDocument PrepareAllViewLookupDefinitionDefinition();
        BsonDocument PrepareAllViewUnwindDefinition();
        BsonDocument PrepareAllViewAddFiledsDefinition();
        BsonDocument PrepareAllViewFinalMatchDefinition();
    }
}