using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.PaymentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule
{
    public interface IClientSubscriptionUpdateService 
    {
        Task<bool> InitiateClientSubscriptionUpdatePaymentProcess(SubscriptionUpdateForClientCommand command);
    }
}
