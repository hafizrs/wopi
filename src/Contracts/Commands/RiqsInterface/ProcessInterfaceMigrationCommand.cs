using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class ProcessInterfaceMigrationCommand
    {
        [Required]
        public string SiteId { get; set; }
        [Required]
        public string[] FolderIds { get; set; }
        [Required]
        public string[] FileIds { get; set; }
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string Context { get; set; }
        public string ActionName { get; set; }

    }
}
