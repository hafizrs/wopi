using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IPricingSeedDataService
    {
        Task<PricingSeedDataResponse> GetPricingSeedData();
    }
}
