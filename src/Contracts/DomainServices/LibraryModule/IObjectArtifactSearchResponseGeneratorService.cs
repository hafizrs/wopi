using MongoDB.Bson;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactSearchResponseGeneratorService
    {
        List<IDictionary<string, object>> PrepareArtifactResponse(List<BsonDocument> documents, ObjectArtifactSearchCommand command);
        List<IDictionary<string, object>> PrepareFormResponseArtifactResponse(List<RiqsObjectArtifact> foundArtifacts, bool isCountRestricted = false, ArtifactResponseEntityDictionary entityDictionary = null);
        Task<ArtifactResponseEntityDictionary> GetDependentArtifactResponseProperties(List<BsonDocument> documents, string organizationId = null);
    }
}