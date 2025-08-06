using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface ISharePointToMongoService
    {
        Task<bool> TransferFileToMongo(string sharePointSite, string filePath);
    }
}
