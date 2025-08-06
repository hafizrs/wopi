using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventHandlers.Services.EmailService
{
   public interface IInvoiceEmailNotifierService
    {
        Task<bool> SendPaymentCompleteEmail(List<string> emailList, Dictionary<string, string> dataContext, IEnumerable<string> invoiceId, string emailTemplate = null);
    }
}
