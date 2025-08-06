using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IInvoiceGeneratorService
    {
        Task CreateInvoiceTemplate(PaymentInvoiceData paymentInvoiceData);
    }
}
