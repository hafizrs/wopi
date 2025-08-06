using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class DownloadEquipemtInterfaceDataCommand
    {
        [Required]
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public string Language { get; set; } = "en";
    }
}
