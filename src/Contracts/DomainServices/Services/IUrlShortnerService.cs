using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices
{
    public interface IUrlShortnerService
    {
        Task<CommandResponse> ShortenUriAsync(string shortUriId, string shortUri);
    }
}
