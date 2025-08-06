using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SubscriptionRenewEventModel
    {
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string SubscriptionId { get; set; }
        public string NotificationId { get; set; }
    }
}
