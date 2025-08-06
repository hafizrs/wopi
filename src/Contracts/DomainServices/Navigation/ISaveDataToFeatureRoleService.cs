using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation
{
    public interface ISaveDataToFeatureRoleService
    {
        Task<bool> SaveData(List<NavInfo> navigationDataList, string navRole);
    }
}
