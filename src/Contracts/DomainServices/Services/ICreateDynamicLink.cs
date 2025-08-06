using Selise.Ecap.SC.Wopi.Contracts.Models;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices
{
    public interface ICreateDynamicLink
    {
        string CreateLink(string url, DynamicLinkGeneratePayload payload);
    }
}
