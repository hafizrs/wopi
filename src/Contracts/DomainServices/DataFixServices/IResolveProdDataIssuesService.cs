using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices
{
    public interface IResolveProdDataIssuesService
    {
        Task<bool> InitiateFix(ResolveProdDataIssuesCommand command);
    }
}