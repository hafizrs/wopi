using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceManagerService
    {
        Task<List<GetSiteResponse>> GetSites();
        Task<List<GetItemsResponse>> GetItems(string siteId, string itemId = null);
    }
}
