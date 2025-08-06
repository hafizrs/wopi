using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface IDeleteDataByCollectionSpecific
    {
        Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string orgId = null);
    }
}
