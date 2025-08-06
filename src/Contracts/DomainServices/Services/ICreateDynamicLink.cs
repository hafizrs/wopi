using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ICreateDynamicLink
    {
        string CreateLink(string url, DynamicLinkGeneratePayload payload);
    }
}
