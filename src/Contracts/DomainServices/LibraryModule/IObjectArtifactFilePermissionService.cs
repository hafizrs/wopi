using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFilePermissionService
    {
        Task<bool> SetObjectArtifactFilePermissions(ObjectArtifact objectArtifact, ObjectArtifactEvent eventName);
        Dictionary<string, object> PrepareObjectArtifactPermissionModel(ObjectArtifact objectArtifact, ObjectArtifactEvent eventName);
    }
}