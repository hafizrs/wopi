using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisOrganizationCreateUpdateEventService
    {
        Task<bool> InitiateOrganizationCreateUpdateAfterEffects(PraxisOrganization previousOrgData, string eventType);
    }
}
