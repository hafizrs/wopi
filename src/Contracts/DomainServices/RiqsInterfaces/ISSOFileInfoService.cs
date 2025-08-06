using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface ISSOFileInfoService
    {
        Task<string> GetSSOFileInfo(string sharePointSite, string FilePath);
    }
}
