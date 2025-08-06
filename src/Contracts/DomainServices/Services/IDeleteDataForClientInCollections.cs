using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IDeleteDataForClientInCollections
    {
        Task DeleteDataForClient(string clientId, string orgId = null);
    }
}