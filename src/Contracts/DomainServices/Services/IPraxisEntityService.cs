using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisEntityService
    {
        public Task<bool> SetReadPermissionForEntity(SetReadPermissionForEntityCommand command);
    }
}