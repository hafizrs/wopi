using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SendPaymnetNotificationPayload
    {
        public string NotificationSubscriptionId { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
    }
}
