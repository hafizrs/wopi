using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateClientSubscriptionInformationCommand
    {
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string PaymentDetailId { get; set; }
        public List<NavInfo> NavigationList { get; set; }
        public IEnumerable<NavigationDto> Navigations { get; set; }
        public string ActionName { get; set; }
        public string Context { get; set; }
        public string NotificationSubscriptionId { get; set; }
    }
}
