using System.Collections.Generic;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IConnectionService
    {
        Task<List<Connection>> GetConnections(string ParentEntityId, string[] tags);
        Task<Connection> GetParentEntity(string ChildEntityId, string[] tags);
    }
}
