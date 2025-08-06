using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices
{
    public interface IPraxisEmailDataBuilders
    {
        Dictionary<string, string> BuildCreateOrganizationEmailData(string paymentInitializeId, string personName);
    }
}
