using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceEquipmentService
    {
        Task CreateEquimentFromRiqsInterfaceMigration(CreateEquimentFromRiqsInterfaceMigrationCommand command);
    }
}
