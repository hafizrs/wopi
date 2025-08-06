using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFolderPermissionService
    {
        Task<bool> SetObjectArtifactFolderPermissions(ObjectArtifact objectArtifact);
        Dictionary<string, object> PrepareObjectArtifactFolderPermissionModel(ObjectArtifact objectArtifact);
    }
}