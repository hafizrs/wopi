using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation
{
    public interface IDynamicNavigationPreparation
    {
        Task<bool> ProcessNavigationData(string organizationId, List<NavInfo> navigationList);
    }
}
