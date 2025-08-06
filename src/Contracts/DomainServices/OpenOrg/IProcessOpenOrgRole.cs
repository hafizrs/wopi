using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg
{
    public interface IProcessOpenOrgRole
    {
        OpenOrganizationResponse ProcessRole(string clientId, bool IsOpenOrganization);
    }
}
