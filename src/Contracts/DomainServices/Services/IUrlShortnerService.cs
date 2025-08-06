using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUrlShortnerService
    {
        Task<CommandResponse> ShortenUriAsync(string shortUriId, string shortUri);
    }
}
