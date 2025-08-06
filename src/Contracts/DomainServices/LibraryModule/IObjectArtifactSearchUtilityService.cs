using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactSearchUtilityService
    {
        Task<List<ObjectArtifact>> GetParentWithAllChildrenObjectArtifacts(string objectArtifactId);
        Task<List<ObjectArtifact>> GetAllChildrenObjectArtifacts(string objectArtifactId);
        BsonDocument PrepareRegExTextSearchFilter(string propertyName, string inputText);
    }
}