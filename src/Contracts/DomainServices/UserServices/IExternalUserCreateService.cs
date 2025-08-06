using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices
{
    public interface IExternalUserCreateService
    {
        Task ProcessDataForCirsReport(CirsGenericReport report, PraxisClient client, string relatedEntityName);
    }
}
