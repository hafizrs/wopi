using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IAppCatalogueRepositoryService
    {
        IEnumerable<FeatureRoleMap> GetFeatureRoleMapsByRoles(IEnumerable<string> roles);

        IEnumerable<AppResponse> GetFeatureRoles();
    }
}
