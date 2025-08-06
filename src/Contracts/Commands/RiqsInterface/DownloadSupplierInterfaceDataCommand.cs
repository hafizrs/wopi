using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class DownloadSupplierInterfaceDataCommand
    {
        [Required]
        public string ClientId { get; set; }
        public string Language { get; set; } = "en";
    }
}
