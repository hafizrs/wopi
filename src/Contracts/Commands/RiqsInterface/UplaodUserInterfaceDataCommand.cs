using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class UplaodUserInterfaceDataCommand
    {
        [Required]
        public string FileId { get; set; }
        [Required]
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public string Context { get; set; }
        public string ActionName { get; set; }
        public string MigrationSummaryId { get; set; }
        public bool IsUpdate { get; set; } = false; 
    }
}
