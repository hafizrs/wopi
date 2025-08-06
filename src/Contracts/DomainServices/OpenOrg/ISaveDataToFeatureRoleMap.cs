using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg
{
    public interface ISaveDataToFeatureRoleMap
    {
        bool ProcessData(List<PraxisDeleteFeature> deleteFeatureList, string role);
    }
}
