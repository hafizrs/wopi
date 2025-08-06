using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
   public interface ISubscriptionInstallmentPaymentCalculationService
    {
        Task<List<CalculatedInstallmentPaymentModel>> GetCalculatedSubscriptionInstallmentPayment(int durationOfSubscription, double totalAmount); 
    }
}
