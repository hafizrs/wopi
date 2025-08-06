using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IObjectArifactVersionService
    {
        string GenerateParentVersionIfParentArtifactIsNullOrEmpty();
        bool GenerateParentVersionWithLibraryAdminIfParentArtifactIsNotEmpty();
    }
}
